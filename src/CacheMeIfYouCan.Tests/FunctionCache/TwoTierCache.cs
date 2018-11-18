﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheMeIfYouCan.Notifications;
using Xunit;

namespace CacheMeIfYouCan.Tests.FunctionCache
{
    public class TwoTierCache
    {
        [Fact]
        public async Task ChecksLocalFirstThenDistributed()
        {
            var distributedCache = new TestCache<string, string>(x => x, x => x);
            var localCache1 = new TestLocalCache<string, string>();
            var localCache2 = new TestLocalCache<string, string>();
            
            Func<string, Task<string>> echo = new Echo();
            
            var results1 = new List<FunctionCacheGetResult>();
            var results2 = new List<FunctionCacheGetResult>();
            
            var cachedEcho1 = echo
                .Cached()
                .WithLocalCache(localCache1)
                .WithDistributedCache(distributedCache)
                .OnResult(results1.Add)
                .Build();
            
            var cachedEcho2 = echo
                .Cached()
                .WithLocalCache(localCache2)
                .WithDistributedCache(distributedCache)
                .OnResult(results2.Add)
                .Build();

            await cachedEcho1("123");
            Assert.Single(results1);
            Assert.Equal(Outcome.Fetch, results1[0].Results.Single().Outcome);

            await cachedEcho1("123");
            Assert.Equal(2, results1.Count);
            Assert.Equal(Outcome.FromCache, results1[1].Results.Single().Outcome);
            Assert.Equal(localCache1.CacheType, results1[1].Results.Single().CacheType);

            await cachedEcho2("123");
            Assert.Single(results2);
            Assert.Equal(Outcome.FromCache, results2[0].Results.Single().Outcome);
            Assert.Equal(distributedCache.CacheType, results2[0].Results.Single().CacheType);
            
            await cachedEcho2("123");
            Assert.Equal(2, results2.Count);
            Assert.Equal(Outcome.FromCache, results2[1].Results.Single().Outcome);
            Assert.Equal(localCache2.CacheType, results2[1].Results.Single().CacheType);
        }

        [Fact]
        public async Task DistributedCacheIsAbleToRemoveKeysFromLocalCache()
        {
            var localCache = new TestLocalCache<string, string>();
            var distributedCache = new TestCache<string, string>(x => x, x => x, localCache.Remove);

            Func<string, Task<string>> echo = new Echo();

            var results = new List<FunctionCacheGetResult>();

            var cachedEcho = echo
                .Cached()
                .WithLocalCache(localCache)
                .WithDistributedCache(distributedCache)
                .OnResult(results.Add)
                .Build();

            await cachedEcho("123");
            Assert.Single(results);
            Assert.Equal(Outcome.Fetch, results[0].Results.Single().Outcome);

            await cachedEcho("123");
            Assert.Equal(2, results.Count);
            Assert.Equal(Outcome.FromCache, results[1].Results.Single().Outcome);
            Assert.Equal(localCache.CacheType, results[1].Results.Single().CacheType);

            distributedCache.OnKeyChangedRemotely("123");

            await cachedEcho("123");
            Assert.Equal(3, results.Count);
            Assert.Equal(Outcome.Fetch, results[2].Results.Single().Outcome);
        }
    }
}