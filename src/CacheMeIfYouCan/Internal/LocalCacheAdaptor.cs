﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CacheMeIfYouCan.Internal
{
    internal class LocalCacheAdaptor<TK, TV> : ICache<TK, TV>
    {
        private readonly ILocalCache<TK, TV> _cache;

        public LocalCacheAdaptor(ILocalCache<TK, TV> cache)
        {
            _cache = cache;

            CacheName = cache.CacheName;
            CacheType = cache.CacheType;
        }

        public string CacheName { get; }
        public string CacheType { get; }

        public Task<IList<GetFromCacheResult<TK, TV>>> Get(ICollection<Key<TK>> keys)
        {
            return Task.FromResult(_cache.Get(keys));
        }

        public Task Set(ICollection<KeyValuePair<Key<TK>, TV>> values, TimeSpan timeToLive)
        {
            _cache.Set(values, timeToLive);
            
            return Task.CompletedTask;
        }
    }
}