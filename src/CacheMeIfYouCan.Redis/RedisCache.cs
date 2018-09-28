﻿using System;
using System.Threading.Tasks;
using CacheMeIfYouCan.Caches;
using StackExchange.Redis;

namespace CacheMeIfYouCan.Redis
{
    internal class RedisCache<TK, TV> : ICache<TK, TV>
    {
        private const string CacheType = "Redis";
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly int _database;
        private readonly string _keySpacePrefix;
        private readonly Func<string, TK> _keyDeserializer;
        private readonly Func<TV, string> _serializer;
        private readonly Func<string, TV> _deserializer;
        private readonly Action<Key<TK>> _removeFromLocalCacheCallback;
        private readonly Func<string, string> _toRedisKey;
        private readonly Func<string, string> _fromRedisKey;
        private readonly RecentlySetKeysManager _recentlySetKeysManager;

        public RedisCache(
            IConnectionMultiplexer multiplexer,
            int database,
            string keySpacePrefix,
            Func<string, TK> keyDeserializer,
            Func<TV, string> serializer,
            Func<string, TV> deserializer,
            Action<Key<TK>> removeFromLocalCacheCallback = null)
        {
            _multiplexer = multiplexer;
            _database = database;
            _keySpacePrefix = keySpacePrefix;
            _keyDeserializer = keyDeserializer;
            _serializer = serializer;
            _deserializer = deserializer;
            _removeFromLocalCacheCallback = removeFromLocalCacheCallback;
            
            if (String.IsNullOrWhiteSpace(keySpacePrefix))
            {
                _toRedisKey = k => k;
                _fromRedisKey = k => k;
            }
            else
            {
                _toRedisKey = k => $"{keySpacePrefix}_{k}";
                _fromRedisKey = k => k.Substring(keySpacePrefix.Length + 1);
            }

            if (_removeFromLocalCacheCallback != null)
            {
                _recentlySetKeysManager = new RecentlySetKeysManager();

                // All Redis instances must have keyevent notifications enabled (eg. 'notify-keyspace-events AE')
                var subscriber = multiplexer.GetSubscriber();

                var keyEvents = new[]
                {
                    "set",
                    "del",
                    "expired",
                    "evicted"
                };

                foreach (var keyEvent in keyEvents)
                    subscriber.Subscribe($"__keyevent@{_database}__:{keyEvent}", (c, k) => RemoveKeyFromMemoryIfNotRecentlySet(k));
            }
        }

        public async Task<GetFromCacheResult<TV>> Get(Key<TK> key)
        {
            var redisDb = GetDatabase();
            var stringKey = key.AsString.Value;
            var redisKey = _toRedisKey(stringKey);

            var fromRedis = await redisDb.StringGetWithExpiryAsync(redisKey);
            
            if (!fromRedis.Value.HasValue)
                return GetFromCacheResult<TV>.NotFound;

            var value = _deserializer(fromRedis.Value);
            var timeToLive = fromRedis.Expiry.GetValueOrDefault();

            return new GetFromCacheResult<TV>(value, timeToLive, CacheType);
        }

        public async Task Set(Key<TK> key, TV value, TimeSpan timeToLive)
        {
            var redisDb = GetDatabase();
            var stringKey = key.AsString.Value;
            var redisKey = _toRedisKey(stringKey);

            var serializedValue = _serializer(value);

            _recentlySetKeysManager?.Mark(stringKey);
            
            await redisDb.StringSetAsync(redisKey, serializedValue, timeToLive);
        }

        private void RemoveKeyFromMemoryIfNotRecentlySet(string redisKey)
        {
            // Ignore keys that are not from the same keyspace
            if (!String.IsNullOrWhiteSpace(_keySpacePrefix) && !redisKey.StartsWith(_keySpacePrefix))
                return;

            var stringKey = _fromRedisKey(redisKey);
            
            if (_recentlySetKeysManager.IsRecentlySet(stringKey))
                return;

            var key = new Key<TK>(_keyDeserializer(stringKey), stringKey);

            _removeFromLocalCacheCallback(key);
        }

        private IDatabase GetDatabase()
        {
            return _multiplexer.GetDatabase(_database);
        }
    }
}