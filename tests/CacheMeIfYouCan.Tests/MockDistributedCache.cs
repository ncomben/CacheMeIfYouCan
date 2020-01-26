﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheMeIfYouCan.LocalCaches;

namespace CacheMeIfYouCan.Tests
{
    public class MockDistributedCache<TKey, TValue> : IDistributedCache<TKey, TValue>
    {
        private readonly ILocalCache<TKey, (TValue, DateTime)> _innerCache = new MemoryCache<TKey, (TValue, DateTime)>(k => k.ToString());
        
        public int TryGetExecutionCount;
        public int SetExecutionCount;
        public int GetManyExecutionCount;
        public int SetManyExecutionCount;
        public int HitsCount;
        public int MissesCount;
        private bool _throwExceptionOnNextAction;

        public Task<(bool Success, ValueAndTimeToLive<TValue> Value)> TryGet(TKey key)
        {
            Interlocked.Increment(ref TryGetExecutionCount);

            if (_throwExceptionOnNextAction)
            {
                _throwExceptionOnNextAction = false;
                throw new Exception();
            }
            
            if (_innerCache.TryGet(key, out var value))
            {
                Interlocked.Increment(ref HitsCount);
                return Task.FromResult((true, new ValueAndTimeToLive<TValue>(value.Item1, value.Item2 - DateTime.UtcNow)));
            }
            
            Interlocked.Increment(ref MissesCount);
            return Task.FromResult((false, new ValueAndTimeToLive<TValue>()));
        }

        public Task Set(TKey key, TValue value, TimeSpan timeToLive)
        {
            Interlocked.Increment(ref SetExecutionCount);
            
            if (_throwExceptionOnNextAction)
            {
                _throwExceptionOnNextAction = false;
                throw new Exception();
            }
            
            _innerCache.Set(key, (value, DateTime.UtcNow + timeToLive), timeToLive);
            
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<KeyValuePair<TKey, ValueAndTimeToLive<TValue>>>> GetMany(IReadOnlyCollection<TKey> keys)
        {
            Interlocked.Increment(ref GetManyExecutionCount);
            
            if (_throwExceptionOnNextAction)
            {
                _throwExceptionOnNextAction = false;
                throw new Exception();
            }
            
            var values = _innerCache
                .GetMany(keys)
                .ToDictionary(kv => kv.Key, kv => new ValueAndTimeToLive<TValue>(kv.Value.Item1, kv.Value.Item2 - DateTime.UtcNow));

            var hits = values.Count;
            var misses = keys.Count - values.Count;

            if (hits > 0) Interlocked.Add(ref HitsCount, hits);
            if (misses > 0) Interlocked.Add(ref MissesCount, misses);
                
            return Task.FromResult<IReadOnlyCollection<KeyValuePair<TKey, ValueAndTimeToLive<TValue>>>>(values);
        }

        public Task SetMany(IReadOnlyCollection<KeyValuePair<TKey, TValue>> values, TimeSpan timeToLive)
        {
            Interlocked.Increment(ref SetManyExecutionCount);
            
            if (_throwExceptionOnNextAction)
            {
                _throwExceptionOnNextAction = false;
                throw new Exception();
            }
            
            _innerCache.SetMany(values.ToDictionary(kv => kv.Key, kv => (kv.Value, DateTime.UtcNow + timeToLive)), timeToLive);
            
            return Task.CompletedTask;
        }

        public void ThrowExceptionOnNextAction() => _throwExceptionOnNextAction = true;
    }
    
    public class MockDistributedCache<TOuterKey, TInnerKey, TValue> : IDistributedCache<TOuterKey, TInnerKey, TValue>
    {
        private readonly ILocalCache<TOuterKey, TInnerKey, (TValue, DateTime)> _innerCache = new MemoryCache<TOuterKey, TInnerKey, (TValue, DateTime)>(k => k.ToString(), k => k.ToString());

        public int GetManyExecutionCount;
        public int SetManyExecutionCount;
        public int HitsCount;
        public int MissesCount;
        private bool _throwExceptionOnNextAction;

        public Task<IReadOnlyCollection<KeyValuePair<TInnerKey, ValueAndTimeToLive<TValue>>>> GetMany(TOuterKey outerKey, IReadOnlyCollection<TInnerKey> innerKeys)
        {
            Interlocked.Increment(ref GetManyExecutionCount);

            if (_throwExceptionOnNextAction)
            {
                _throwExceptionOnNextAction = false;
                throw new Exception();
            }
            
            var values = _innerCache
                .GetMany(outerKey, innerKeys)
                .ToDictionary(kv => kv.Key, kv => new ValueAndTimeToLive<TValue>(kv.Value.Item1, kv.Value.Item2 - DateTime.UtcNow));
            
            var hits = values.Count;
            var misses = innerKeys.Count - values.Count;

            if (hits > 0) Interlocked.Add(ref HitsCount, hits);
            if (misses > 0) Interlocked.Add(ref MissesCount, misses);

            return Task.FromResult<IReadOnlyCollection<KeyValuePair<TInnerKey, ValueAndTimeToLive<TValue>>>>(values);
        }

        public Task SetMany(TOuterKey outerKey, IReadOnlyCollection<KeyValuePair<TInnerKey, TValue>> values, TimeSpan timeToLive)
        {
            Interlocked.Increment(ref SetManyExecutionCount);
            
            if (_throwExceptionOnNextAction)
            {
                _throwExceptionOnNextAction = false;
                throw new Exception();
            }
            
            _innerCache.SetMany(outerKey, values.ToDictionary(kv => kv.Key, kv => (kv.Value, DateTime.UtcNow + timeToLive)), timeToLive);

            return Task.CompletedTask;
        }

        public void ThrowExceptionOnNextAction() => _throwExceptionOnNextAction = true;
    }
}