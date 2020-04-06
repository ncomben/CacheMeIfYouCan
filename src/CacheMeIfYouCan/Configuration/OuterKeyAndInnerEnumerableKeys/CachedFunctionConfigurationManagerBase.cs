﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CacheMeIfYouCan.Events.CachedFunction.OuterKeyAndInnerEnumerableKeys;
using CacheMeIfYouCan.Internal;
using CacheMeIfYouCan.Internal.CachedFunctions;
using CacheMeIfYouCan.Internal.CachedFunctions.Configuration;
using CacheMeIfYouCan.Internal.RequestConverters;
using CacheMeIfYouCan.Internal.ResponseConverters;

namespace CacheMeIfYouCan.Configuration.OuterKeyAndInnerEnumerableKeys
{
    public abstract class CachedFunctionConfigurationManagerBase<TParams, TOuterKey, TInnerKeys, TResponse, TInnerKey, TValue, TConfig>
        where TConfig : CachedFunctionConfigurationManagerBase<TParams, TOuterKey, TInnerKeys, TResponse, TInnerKey, TValue, TConfig>
        where TInnerKeys : IEnumerable<TInnerKey>
        where TResponse : IEnumerable<KeyValuePair<TInnerKey, TValue>>
    {
        private readonly CachedFunctionWithOuterKeyAndInnerEnumerableKeysConfiguration<TParams, TOuterKey, TInnerKey, TValue> _config =
            new CachedFunctionWithOuterKeyAndInnerEnumerableKeysConfiguration<TParams, TOuterKey, TInnerKey, TValue>();

        private Func<IReadOnlyCollection<TInnerKey>, TInnerKeys> _requestConverter;
        private Func<Dictionary<TInnerKey, TValue>, TResponse> _responseConverter;

        public TConfig WithTimeToLive(TimeSpan timeToLive)
        {
            _config.TimeToLive = timeToLive;
            return (TConfig)this;
        }
        
        private protected TConfig WithTimeToLiveFactoryInternal(Func<TParams, IReadOnlyCollection<TInnerKey>, TimeSpan> timeToLiveFactory)
        {
            _config.TimeToLiveFactory = timeToLiveFactory;
            return (TConfig)this;
        }
        
        public TConfig WithLocalCache(ILocalCache<TOuterKey, TInnerKey, TValue> cache)
        {
            _config.LocalCache = cache;
            return (TConfig)this;
        }

        public TConfig WithDistributedCache(IDistributedCache<TOuterKey, TInnerKey, TValue> cache)
        {
            _config.DistributedCache = cache;
            return (TConfig)this;
        }

        public TConfig DisableCaching(bool disableCaching = true)
        {
            _config.DisableCaching = disableCaching;
            return (TConfig)this;
        }

        public TConfig FillMissingKeys(TValue value)
        {
            _config.FillMissingKeysConstantValue = (true, value);
            return (TConfig)this;
        }

        public TConfig FillMissingKeys(Func<TOuterKey, TInnerKey, TValue> valueFactory)
        {
            _config.FillMissingKeysValueFactory = valueFactory;
            return (TConfig)this;
        }

        public TConfig SkipCacheWhen(
            Func<TOuterKey, bool> predicate,
            SkipCacheWhen when = CacheMeIfYouCan.SkipCacheWhen.SkipCacheGetAndCacheSet)
        {
            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheGet))
                _config.SkipCacheGetPredicateOuterKeyOnly = _config.SkipCacheGetPredicateOuterKeyOnly.Or(predicate);

            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheSet))
                _config.SkipCacheSetPredicateOuterKeyOnly = _config.SkipCacheSetPredicateOuterKeyOnly.Or(predicate);
            
            return (TConfig)this;
        }
        
        public TConfig SkipCacheWhen(
            Func<TOuterKey, TInnerKey, bool> predicate,
            SkipCacheWhen when = CacheMeIfYouCan.SkipCacheWhen.SkipCacheGetAndCacheSet)
        {
            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheGet))
                _config.SkipCacheGetPredicate = _config.SkipCacheGetPredicate.Or(predicate);

            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheSet))
                _config.SkipCacheSetPredicate = _config.SkipCacheSetPredicate.Or((outerKey, innerKey, value) => predicate(outerKey, innerKey));

            return (TConfig)this;
        }

        public TConfig SkipCacheWhen(Func<TOuterKey, TInnerKey, TValue, bool> predicate)
        {
            _config.SkipCacheSetPredicate = _config.SkipCacheSetPredicate.Or(predicate);
            return (TConfig)this;
        }

        public TConfig SkipLocalCacheWhen(
            Func<TOuterKey, bool> predicate,
            SkipCacheWhen when = CacheMeIfYouCan.SkipCacheWhen.SkipCacheGetAndCacheSet)
        {
            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheGet))
                _config.SkipLocalCacheGetPredicateOuterKeyOnly = _config.SkipLocalCacheGetPredicateOuterKeyOnly.Or(predicate);

            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheSet))
                _config.SkipLocalCacheSetPredicateOuterKeyOnly = _config.SkipLocalCacheSetPredicateOuterKeyOnly.Or(predicate);

            return (TConfig)this;
        }
        
        public TConfig SkipLocalCacheWhen(
            Func<TOuterKey, TInnerKey, bool> predicate,
            SkipCacheWhen when = CacheMeIfYouCan.SkipCacheWhen.SkipCacheGetAndCacheSet)
        {
            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheGet))
                _config.SkipLocalCacheGetPredicate = _config.SkipLocalCacheGetPredicate.Or(predicate);

            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheSet))
                _config.SkipLocalCacheSetPredicate = _config.SkipLocalCacheSetPredicate.Or((outerKey, innerKey, value) => predicate(outerKey, innerKey));

            return (TConfig)this;
        }

        public TConfig SkipLocalCacheWhen(Func<TOuterKey, TInnerKey, TValue, bool> predicate)
        {
            _config.SkipLocalCacheSetPredicate = _config.SkipLocalCacheSetPredicate.Or(predicate);
            return (TConfig)this;
        }
        
        public TConfig SkipDistributedCacheWhen(
            Func<TOuterKey, bool> predicate,
            SkipCacheWhen when = CacheMeIfYouCan.SkipCacheWhen.SkipCacheGetAndCacheSet)
        {
            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheGet))
                _config.SkipDistributedCacheGetPredicateOuterKeyOnly = _config.SkipDistributedCacheGetPredicateOuterKeyOnly.Or(predicate);

            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheSet))
                _config.SkipDistributedCacheSetPredicateOuterKeyOnly = _config.SkipDistributedCacheSetPredicateOuterKeyOnly.Or(predicate);

            return (TConfig)this;
        }
        
        public TConfig SkipDistributedCacheWhen(
            Func<TOuterKey, TInnerKey, bool> predicate,
            SkipCacheWhen when = CacheMeIfYouCan.SkipCacheWhen.SkipCacheGetAndCacheSet)
        {
            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheGet))
                _config.SkipDistributedCacheGetPredicate = _config.SkipDistributedCacheGetPredicate.Or(predicate);

            if (when.HasFlag(CacheMeIfYouCan.SkipCacheWhen.SkipCacheSet))
                _config.SkipDistributedCacheSetPredicate = _config.SkipDistributedCacheSetPredicate.Or((outerKey, innerKey, value) => predicate(outerKey, innerKey));

            return (TConfig)this;
        }

        public TConfig SkipDistributedCacheWhen(Func<TOuterKey, TInnerKey, TValue, bool> predicate)
        {
            _config.SkipDistributedCacheSetPredicate = _config.SkipDistributedCacheSetPredicate.Or(predicate);
            return (TConfig)this;
        }

        public TConfig WithBatchedFetches(int maxBatchSize, BatchBehaviour batchBehaviour = BatchBehaviour.FillBatchesEvenly)
        {
            if (maxBatchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxBatchSize));
            
            _config.MaxBatchSize = maxBatchSize;
            _config.BatchBehaviour = batchBehaviour;
            return (TConfig)this;
        }

        public TConfig OnResult(
            Action<SuccessfulRequestEvent<TParams, TOuterKey, TInnerKey, TValue>> onSuccess = null,
            Action<ExceptionEvent<TParams, TOuterKey, TInnerKey>> onException = null)
        {
            if (!(onSuccess is null))
                _config.OnSuccessAction += onSuccess;
            
            if (!(onException is null))
                _config.OnExceptionAction += onException;

            return (TConfig)this;
        }

        public TConfig WithRequestConverter(Func<IReadOnlyCollection<TInnerKey>, TInnerKeys> requestConverter)
        {
            _requestConverter = requestConverter;
            return (TConfig)this;
        }

        public TConfig WithResponseConverter(Func<Dictionary<TInnerKey, TValue>, TResponse> responseConverter)
        {
            _responseConverter = responseConverter;
            return (TConfig)this;
        }
        
        private protected Func<IReadOnlyCollection<TInnerKey>, TInnerKeys> GetRequestConverter()
        {
            return _requestConverter ?? DefaultRequestConverterResolver.Get<TInnerKey, TInnerKeys>(_config.KeyComparer);
        }
        
        private protected Func<Dictionary<TInnerKey, TValue>, TResponse> GetResponseConverter()
        {
            return _responseConverter ?? DefaultResponseConverterResolver.Get<TInnerKey, TValue, TResponse>(_config.KeyComparer);
        }

        private protected CachedFunctionWithOuterKeyAndInnerEnumerableKeys<TParams, TOuterKey, TInnerKey, TValue> BuildCachedFunction(
            Func<TParams, IReadOnlyCollection<TInnerKey>, CancellationToken, ValueTask<IEnumerable<KeyValuePair<TInnerKey, TValue>>>> originalFunction,
            Func<TParams, TOuterKey> keySelector)
        {
            return new CachedFunctionWithOuterKeyAndInnerEnumerableKeys<TParams, TOuterKey, TInnerKey, TValue>(
                originalFunction,
                keySelector,
                _config);
        }
    }
}