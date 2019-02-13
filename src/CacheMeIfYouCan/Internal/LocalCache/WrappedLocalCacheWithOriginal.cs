﻿using System;
using System.Collections.Generic;

namespace CacheMeIfYouCan.Internal.LocalCache
{
    internal class WrappedLocalCacheWithOriginal<TK, TV> : ILocalCache<TK, TV>
    {
        private readonly ILocalCache<TK, TV> _wrapped;

        public WrappedLocalCacheWithOriginal(ILocalCache<TK, TV> wrapped, ILocalCache<TK, TV> original)
        {
            _wrapped = wrapped;

            Wrapped = wrapped;
            Original = original;
            CacheName = wrapped.CacheName;
            CacheType = wrapped.CacheType;
        }

        public ILocalCache<TK, TV> Wrapped { get; }
        public ILocalCache<TK, TV> Original { get; }
        public string CacheName { get; }
        public string CacheType { get; }

        public GetFromCacheResult<TK, TV> Get(Key<TK> key) => _wrapped.Get(key);

        public void Set(Key<TK> key, TV value, TimeSpan timeToLive) => _wrapped.Set(key, value, timeToLive);

        public IList<GetFromCacheResult<TK, TV>> Get(ICollection<Key<TK>> keys) => _wrapped.Get(keys);

        public void Set(ICollection<KeyValuePair<Key<TK>, TV>> values, TimeSpan timeToLive) => _wrapped.Set(values, timeToLive);

        public bool Remove(Key<TK> key) => _wrapped.Remove(key);

        public void Dispose() => _wrapped.Dispose();
    }
}