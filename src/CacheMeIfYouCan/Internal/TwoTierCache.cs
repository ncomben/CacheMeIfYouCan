﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheMeIfYouCan.Caches;

namespace CacheMeIfYouCan.Internal
{
    internal class TwoTierCache<TK, TV> : ICache<TK, TV>
    {
        private readonly ILocalCache<TK, TV> _localCache;
        private readonly ICache<TK, TV> _remoteCache;
        private readonly IEqualityComparer<Key<TK>> _keyComparer;

        public TwoTierCache(
            ILocalCache<TK, TV> localCache,
            ICache<TK, TV> remoteCache,
            IEqualityComparer<Key<TK>> keyComparer)
        {
            _localCache = localCache;
            _remoteCache = remoteCache;
            _keyComparer = keyComparer;
            CacheType = $"{_localCache.CacheType}+{_remoteCache.CacheType}";
        }

        public string CacheType { get; }

        public async Task<IList<GetFromCacheResult<TK, TV>>> Get(ICollection<Key<TK>> keys)
        {
            var fromLocalCache = _localCache.Get(keys) ?? new GetFromCacheResult<TK, TV>[0];
            
            if (fromLocalCache.Count == keys.Count)
                return fromLocalCache;

            var remaining = keys
                .Except(fromLocalCache.Select(c => c.Key), _keyComparer)
                .ToArray();

            if (!remaining.Any())
                return fromLocalCache;
            
            var fromRemoteCache = await _remoteCache.Get(remaining);
            
            foreach (var result in fromRemoteCache)
                _localCache.Set(result.Key, result.Value, result.TimeToLive);

            return fromLocalCache
                .Concat(fromRemoteCache)
                .ToArray();
        }

        public async Task Set(ICollection<KeyValuePair<Key<TK>, TV>> values, TimeSpan timeToLive)
        {
            _localCache.Set(values, timeToLive);

            await _remoteCache.Set(values, timeToLive);
        }
    }
}