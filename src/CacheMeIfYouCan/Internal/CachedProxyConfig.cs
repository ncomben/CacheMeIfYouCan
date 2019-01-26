﻿using System;
using System.Collections.Generic;
using CacheMeIfYouCan.Notifications;
using CacheMeIfYouCan.Serializers;

namespace CacheMeIfYouCan.Internal
{
    internal class CachedProxyConfig
    {
        public CachedProxyConfig(
            Type interfaceType,
            KeySerializers keySerializers,
            ValueSerializers valueSerializers,
            EqualityComparers keyComparers,
            TimeSpan? timeToLive,
            bool? earlyFetchEnabled,
            bool? disableCache,
            ILocalCacheFactory localCacheFactory,
            IDistributedCacheFactory distributedCacheFactory,
            Func<CachedProxyFunctionInfo, string> keyspacePrefixFunc,
            Action<FunctionCacheGetResult> onResult,
            Action<FunctionCacheFetchResult> onFetch,
            Action<FunctionCacheException> onException,
            Action<CacheGetResult> onCacheGet,
            Action<CacheSetResult> onCacheSet,
            Action<CacheException> onCacheException,
            string keyParamSeparator,
            IDictionary<MethodInfoKey, object> functionCacheConfigActions)
        {
            InterfaceType = interfaceType;
            KeySerializers = keySerializers;
            ValueSerializers = valueSerializers;
            KeyComparers = keyComparers;
            TimeToLive = timeToLive;
            EarlyFetchEnabled = earlyFetchEnabled;
            DisableCache = disableCache;
            LocalCacheFactory = localCacheFactory;
            DistributedCacheFactory = distributedCacheFactory;
            KeyspacePrefixFunc = keyspacePrefixFunc;
            OnResult = onResult;
            OnFetch = onFetch;
            OnException = onException;
            OnCacheGet = onCacheGet;
            OnCacheSet = onCacheSet;
            OnCacheException = onCacheException;
            KeyParamSeparator = keyParamSeparator;
            FunctionCacheConfigActions = functionCacheConfigActions;
        }
        
        public Type InterfaceType { get; }
        public KeySerializers KeySerializers { get; }
        public ValueSerializers ValueSerializers { get; }
        public EqualityComparers KeyComparers { get; }
        public TimeSpan? TimeToLive { get; }
        public bool? EarlyFetchEnabled { get; }
        public bool? DisableCache { get; }
        public ILocalCacheFactory LocalCacheFactory { get; }
        public IDistributedCacheFactory DistributedCacheFactory { get; }
        public Func<CachedProxyFunctionInfo, string> KeyspacePrefixFunc { get; }
        public Action<FunctionCacheGetResult> OnResult { get; }
        public Action<FunctionCacheFetchResult> OnFetch { get; }
        public Action<FunctionCacheException> OnException { get; }
        public Action<CacheGetResult> OnCacheGet { get; }
        public Action<CacheSetResult> OnCacheSet { get; }
        public Action<CacheException> OnCacheException { get; }
        public string KeyParamSeparator { get; }
        public IDictionary<MethodInfoKey, object> FunctionCacheConfigActions { get; }
    }
}