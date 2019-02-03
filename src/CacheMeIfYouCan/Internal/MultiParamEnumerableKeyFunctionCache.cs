﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheMeIfYouCan.Internal.DuplicateTaskCatcher;
using CacheMeIfYouCan.Notifications;

namespace CacheMeIfYouCan.Internal
{
    internal sealed class MultiParamEnumerableKeyFunctionCache<TK1, TK2, TV> : IPendingRequestsCounter, IDisposable
    {
        private readonly ICacheInternal<(TK1, TK2), TV> _cache;
        private readonly TimeSpan _timeToLive;
        private readonly Func<TK1, string> _outerKeySerializer;
        private readonly Func<TK2, string> _innerKeySerializer;
        private readonly Func<TV> _defaultValueFactory;
        private readonly bool _continueOnException;
        private readonly Action<FunctionCacheGetResult<(TK1, TK2), TV>> _onResult;
        private readonly Action<FunctionCacheFetchResult<(TK1, TK2), TV>> _onFetch;
        private readonly Action<FunctionCacheException<(TK1, TK2)>> _onException;
        private readonly KeyComparer<TK2> _innerKeyComparer;
        private readonly IEqualityComparer<Key<(TK1, TK2)>> _tupleKeysOnlyDifferingBySecondItemComparer;
        private readonly string _keyParamSeparator;
        private readonly int _maxFetchBatchSize;
        private readonly DuplicateTaskCatcherCombinedMulti<TK1, TK2, TV> _fetchHandler;
        private int _pendingRequestsCount;
        private bool _disposed;
        
        public MultiParamEnumerableKeyFunctionCache(
            Func<TK1, IEnumerable<TK2>, Task<IDictionary<TK2, TV>>> func,
            string functionName,
            ICacheInternal<(TK1, TK2), TV> cache,
            TimeSpan timeToLive,
            Func<TK1, string> outerKeySerializer,
            Func<TK2, string> innerKeySerializer,
            Func<TV> defaultValueFactory,
            Action<FunctionCacheGetResult<(TK1, TK2), TV>> onResult,
            Action<FunctionCacheFetchResult<(TK1, TK2), TV>> onFetch,
            Action<FunctionCacheException<(TK1, TK2)>> onException,
            KeyComparer<TK1> outerKeyComparer,
            KeyComparer<TK2> innerKeyComparer,
            string keyParamSeparator,
            int maxFetchBatchSize)
        {
            Name = functionName;
            Type = GetType().Name;
            _cache = cache;
            _timeToLive = timeToLive;
            _outerKeySerializer = outerKeySerializer;
            _innerKeySerializer = innerKeySerializer;
            _defaultValueFactory = defaultValueFactory;
            _continueOnException = defaultValueFactory != null;
            _onResult = onResult;
            _onFetch = onFetch;
            _onException = onException;
            _innerKeyComparer = innerKeyComparer;
            _tupleKeysOnlyDifferingBySecondItemComparer = new TupleKeysOnlyDifferingBySecondItemComparer(innerKeyComparer);
            _keyParamSeparator = keyParamSeparator;
            _maxFetchBatchSize = maxFetchBatchSize <= 0 ? Int32.MaxValue : maxFetchBatchSize;
            
            _fetchHandler = new DuplicateTaskCatcherCombinedMulti<TK1, TK2, TV>(
                func,
                outerKeyComparer.Inner,
                innerKeyComparer.Inner);
        }

        public string Name { get; }
        public string Type { get; }
        public int PendingRequestsCount => _pendingRequestsCount;
        
        public void Dispose()
        {
            _disposed = true;
            PendingRequestsCounterContainer.Remove(this);
        }
        
        public async Task<IReadOnlyCollection<IKeyValuePair<(TK1, TK2), TV>>> GetMulti(
            TK1 outerKey,
            IEnumerable<TK2> innerKeys)
        {
            if (_disposed)
                throw new ObjectDisposedException($"{Name} - {Type}");
            
            var timestamp = Timestamp.Now;
            var stopwatchStart = Stopwatch.GetTimestamp();
            
            Dictionary<Key<(TK1, TK2)>, FunctionCacheGetResultInner<(TK1, TK2), TV>> results;
            if (innerKeys is ICollection<TK2> c)
            {
                results = new Dictionary<Key<(TK1, TK2)>, FunctionCacheGetResultInner<(TK1, TK2), TV>>(
                    c.Count,
                    _tupleKeysOnlyDifferingBySecondItemComparer);
            }
            else
            {
                results = new Dictionary<Key<(TK1, TK2)>, FunctionCacheGetResultInner<(TK1, TK2), TV>>(
                    _tupleKeysOnlyDifferingBySecondItemComparer);
            }

            var keys = BuildCombinedKeys(outerKey, innerKeys);

            var error = false;

            IReadOnlyCollection<FunctionCacheGetResultInner<(TK1, TK2), TV>> readonlyResults;
    
            using (SynchronizationContextRemover.StartNew())
            {
                try
                {
                    Interlocked.Increment(ref _pendingRequestsCount);

                    Key<(TK1, TK2)>[] missingKeys = null;
                    if (_cache != null)
                    {
                        var fromCacheTask = _cache.Get(keys);

                        var fromCache = fromCacheTask.IsCompleted
                            ? fromCacheTask.Result
                            : await fromCacheTask;

                        if (fromCache != null && fromCache.Any())
                        {
                            foreach (var result in fromCache)
                            {
                                results[result.Key] = new FunctionCacheGetResultInner<(TK1, TK2), TV>(
                                    result.Key,
                                    result.Value,
                                    Outcome.FromCache,
                                    result.CacheType);
                            }

                            missingKeys = keys
                                .Except(results.Keys, _tupleKeysOnlyDifferingBySecondItemComparer)
                                .ToArray();
                        }
                    }

                    if (missingKeys == null)
                        missingKeys = keys;

                    if (missingKeys.Any())
                    {
                        var fetched = await Fetch(outerKey, missingKeys);

                        if (fetched != null && fetched.Any())
                        {
                            foreach (var result in fetched)
                            {
                                results[result.Key] = new FunctionCacheGetResultInner<(TK1, TK2), TV>(
                                    result.Key,
                                    result.Value,
                                    Outcome.Fetch,
                                    null);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = true;
                    return HandleError(keys, ex);
                }
                finally
                {
#if NET45
                    readonlyResults = results.Values.ToArray();
#else
                    readonlyResults = results.Values;
#endif

                    Interlocked.Decrement(ref _pendingRequestsCount);

                    _onResult?.Invoke(new FunctionCacheGetResult<(TK1, TK2), TV>(
                        Name,
                        readonlyResults,
                        !error,
                        timestamp,
                        StopwatchHelper.GetDuration(stopwatchStart)));
                }
            }

            return readonlyResults;
        }

        private async Task<IList<FunctionCacheFetchResultInner<(TK1, TK2), TV>>> Fetch(
            TK1 outerKey,
            Key<(TK1, TK2)>[] innerKeys)
        {
            if (innerKeys.Length < _maxFetchBatchSize)
                return await FetchBatch(innerKeys);

            var tasks = innerKeys
                .Batch(_maxFetchBatchSize)
                .Select(FetchBatch)
                .ToArray();

            await Task.WhenAll(tasks);

            return tasks
                .SelectMany(t => t.Result)
                .ToArray();
            
            async Task<IList<FunctionCacheFetchResultInner<(TK1, TK2), TV>>> FetchBatch(
                IList<Key<(TK1, TK2)>> batchInnerKeys)
            {
                var timestamp = Timestamp.Now;
                var stopwatchStart = Stopwatch.GetTimestamp();
                var error = false;

                var results = new List<FunctionCacheFetchResultInner<(TK1, TK2), TV>>();

                try
                {
                    var fetched = await _fetchHandler.ExecuteAsync(outerKey, batchInnerKeys.Select(k => k.AsObject.Item2).ToArray());

                    if (fetched != null && fetched.Any())
                    {
                        var keysMap = batchInnerKeys.ToDictionary(k => k.AsObject.Item2, _innerKeyComparer);

                        var nonDuplicates = new List<KeyValuePair<Key<(TK1, TK2)>, TV>>(batchInnerKeys.Count);

                        foreach (var kv in fetched)
                        {
                            var key = keysMap[kv.Key];

                            results.Add(new FunctionCacheFetchResultInner<(TK1, TK2), TV>(
                                key,
                                kv.Value.Value,
                                true,
                                kv.Value.Duplicate,
                                StopwatchHelper.GetDuration(stopwatchStart, kv.Value.StopwatchTimestampCompleted)));

                            if (!kv.Value.Duplicate)
                                nonDuplicates.Add(new KeyValuePair<Key<(TK1, TK2)>, TV>(key, kv.Value.Value));
                        }

                        if (_cache != null)
                        {
                            if (nonDuplicates.Any())
                            {
                                var setValueTask = _cache.Set(nonDuplicates, _timeToLive);

                                if (!setValueTask.IsCompleted)
                                    await setValueTask;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var duration = StopwatchHelper.GetDuration(stopwatchStart);

                    var fetchedKeys = results.Select(r => r.Key);

                    results.AddRange(batchInnerKeys
                        .Except(fetchedKeys, _tupleKeysOnlyDifferingBySecondItemComparer)
                        .Select(k =>
                            new FunctionCacheFetchResultInner<(TK1, TK2), TV>(k, default, false, false, duration)));

                    var exception = new FunctionCacheFetchException<(TK1, TK2)>(
                        Name,
                        batchInnerKeys,
                        Timestamp.Now,
                        "Unable to fetch value(s)",
                        ex);

                    _onException?.Invoke(exception);

                    error = true;
                    throw exception;
                }
                finally
                {
                    _onFetch?.Invoke(new FunctionCacheFetchResult<(TK1, TK2), TV>(
                        Name,
                        results,
                        !error,
                        timestamp,
                        StopwatchHelper.GetDuration(stopwatchStart)));
                }

                return results;
            }
        }

        private IReadOnlyCollection<FunctionCacheGetResultInner<(TK1, TK2), TV>> HandleError(
            IList<Key<(TK1, TK2)>> keys,
            Exception ex)
        {
            var message = _continueOnException
                ? "Unable to get value(s). Default being returned"
                : "Unable to get value(s)";

            var exception = new FunctionCacheGetException<(TK1, TK2)>(
                Name,
                keys,
                Timestamp.Now,
                message,
                ex);
            
            _onException?.Invoke(exception);

            if (!_continueOnException)
                throw exception;
            
            var defaultValue = _defaultValueFactory == null
                ? default
                : _defaultValueFactory();
            
            return keys
                .Select(k => new FunctionCacheGetResultInner<(TK1, TK2), TV>(k, defaultValue, Outcome.Error, null))
                .ToArray();
        }

        private Key<(TK1, TK2)>[] BuildCombinedKeys(TK1 outerKey, IEnumerable<TK2> innerKeys)
        {
            var outerKeySerializer = new Lazy<string>(() => _outerKeySerializer(outerKey));
            
            return innerKeys
                .Select(k => new Key<(TK1, TK2)>((outerKey, k), Serialize))
                .ToArray();

            string Serialize((TK1, TK2) key)
            {
                return outerKeySerializer.Value + _keyParamSeparator + _innerKeySerializer(key.Item2);
            }
        }

        // Use this comparer whenever we know the first component of each key is the same
        private class TupleKeysOnlyDifferingBySecondItemComparer : IEqualityComparer<Key<(TK1, TK2)>>
        {
            private readonly IEqualityComparer<TK2> _comparer;

            public TupleKeysOnlyDifferingBySecondItemComparer(IEqualityComparer<TK2> comparer)
            {
                _comparer = comparer;
            }

            public bool Equals(Key<(TK1, TK2)> x, Key<(TK1, TK2)> y)
            {
                return _comparer.Equals(x.AsObject.Item2, y.AsObject.Item2);
            }

            public int GetHashCode(Key<(TK1, TK2)> obj)
            {
                return _comparer.GetHashCode(obj.AsObject.Item2);
            }
        }
    }
}