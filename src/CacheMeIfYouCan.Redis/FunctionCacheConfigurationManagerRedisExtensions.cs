﻿using System;

namespace CacheMeIfYouCan.Redis
{
    public static class FunctionCacheConfigurationManagerRedisExtensions
    {
        public static FunctionCacheConfigurationManager<TK, TV> WithRedis<TK, TV>(
            this FunctionCacheConfigurationManager<TK, TV> configManager,
            Action<RedisConfig> configAction)
        {
            var config = new RedisConfig();

            configAction(config);
            
            configManager.WithCacheFactory(new RedisCacheFactory(config));
            
            return configManager;
        }
    }
}