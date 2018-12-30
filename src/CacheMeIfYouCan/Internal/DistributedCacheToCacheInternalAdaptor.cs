using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CacheMeIfYouCan.Internal
{
    internal class DistributedCacheToCacheInternalAdapter<TK, TV> : ICacheInternal<TK, TV>
    {
        private readonly IDistributedCache<TK, TV> _cache;

        public DistributedCacheToCacheInternalAdapter(IDistributedCache<TK, TV> cache)
        {
            _cache = cache;

            CacheName = cache.CacheName;
            CacheType = cache.CacheType;
        }

        public string CacheName { get; }
        public string CacheType { get; }
        
        public async ValueTask<GetFromCacheResult<TK, TV>> Get(Key<TK> key)
        {
            return await _cache.Get(key);
        }

        public async ValueTask Set(Key<TK> key, TV value, TimeSpan timeToLive)
        {
            await _cache.Set(key, value, timeToLive);
        }

        public async ValueTask<IList<GetFromCacheResult<TK, TV>>> Get(ICollection<Key<TK>> keys)
        {
            return await _cache.Get(keys);
        }

        public async ValueTask Set(ICollection<KeyValuePair<Key<TK>, TV>> values, TimeSpan timeToLive)
        {
            await _cache.Set(values, timeToLive);
        }
    }
}