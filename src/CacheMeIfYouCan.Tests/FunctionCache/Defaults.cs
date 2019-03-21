﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheMeIfYouCan.Notifications;
using CacheMeIfYouCan.Serializers;
using CacheMeIfYouCan.Tests.Common;
using FluentAssertions;
using Xunit;

namespace CacheMeIfYouCan.Tests.FunctionCache
{
    [Collection(TestCollections.FunctionCache)]
    public class Defaults
    {
        private readonly CacheSetupLock _setupLock;

        public Defaults(CacheSetupLock setupLock)
        {
            _setupLock = setupLock;
        }

        private const string KeyPrefix = "DefaultsTests";
        
        [Fact]
        public async Task DefaultOnResultIsTriggered()
        {
            var results = new List<FunctionCacheGetResult>();

            Func<string, Task<string>> echo = new Echo();
            Func<string, Task<string>> cachedEcho;
            using (_setupLock.Enter(true))
            {
                DefaultSettings.Cache.OnResult(x =>
                {
                    if (x.Results.FirstOrDefault()?.KeyString.StartsWith(KeyPrefix) ?? false)
                        results.Add(x);
                });

                cachedEcho = echo
                    .Cached()
                    .Build();

                DefaultSettings.Cache.OnResult(null, AdditionBehaviour.Overwrite);
            }

            for (var i = 1; i < 10; i++)
            {
                var key = GetRandomKey();

                await cachedEcho(key);
                Assert.Equal(i, results.Count);
            }
        }
        
        [Fact]
        public async Task DefaultOnFetchIsTriggered()
        {
            var results = new List<FunctionCacheFetchResult>();

            Func<string, Task<string>> echo = new Echo();
            Func<string, Task<string>> cachedEcho;
            using (_setupLock.Enter(true))
            {
                DefaultSettings.Cache.OnFetch(x =>
                {
                    if (x.Results.FirstOrDefault()?.KeyString.StartsWith(KeyPrefix) ?? false)
                        results.Add(x);
                });

                cachedEcho = echo
                    .Cached()
                    .Build();

                DefaultSettings.Cache.OnFetch(null, AdditionBehaviour.Overwrite);
            }

            for (var i = 1; i < 10; i++)
            {
                var key = GetRandomKey();

                await cachedEcho(key);
                Assert.Equal(i, results.Count);
            }
        }
        
        [Fact]
        public async Task DefaultOnExceptionIsTriggered()
        {
            var errors = new List<FunctionCacheException>();

            var count = 0;
            
            Func<string, Task<string>> echo = new Echo(TimeSpan.Zero, x => count++ % 2 == 0);
            Func<string, Task<string>> cachedEcho;
            using (_setupLock.Enter(true))
            {
                DefaultSettings.Cache.OnException(x =>
                {
                    if (x.Keys.FirstOrDefault()?.StartsWith(KeyPrefix) ?? false)
                        errors.Add(x);
                });

                cachedEcho = echo
                    .Cached()
                    .Build();

                DefaultSettings.Cache.OnException(null, AdditionBehaviour.Overwrite);
            }

            var previousErrorCount = 0;
            for (var i = 0; i < 10; i++)
            {
                var key = GetRandomKey();
                
                if (i % 2 == 0)
                {
                    await Assert.ThrowsAnyAsync<FunctionCacheException>(() => cachedEcho(key));
                    Assert.Equal(previousErrorCount += 2, errors.Count); // one for failing the fetch, one for failing the get
                    Assert.Equal(key, errors[errors.Count - 1].Keys.Single());
                    Assert.Equal(key, errors[errors.Count - 2].Keys.Single());
                }
                else
                {
                    Assert.Equal(key, await cachedEcho(key));
                }
            }
        }
        
        [Fact]
        public async Task DefaultOnCacheGetIsTriggered()
        {
            var results = new List<CacheGetResult>();

            var key = Guid.NewGuid().ToString();
            
            Func<string, Task<string>> echo = new Echo();
            Func<string, Task<string>> cachedEcho;
            using (_setupLock.Enter(true))
            {
                DefaultSettings.Cache.OnCacheGet(x =>
                {
                    if (x.Hits.Contains(key) || x.Misses.Contains(key))
                        results.Add(x);
                });

                cachedEcho = echo
                    .Cached()
                    .Build();

                DefaultSettings.Cache.OnCacheGet(null, AdditionBehaviour.Overwrite);
            }

            await cachedEcho(key);

            results.Should().ContainSingle();
            Assert.True(results.Single().Success);
            Assert.Empty(results.Single().Hits);
            results.Single().Misses.Should().ContainSingle();
            Assert.Equal(key, results.Single().Misses.Single());
            Assert.Equal("memory", results.Single().CacheType);
            
            for (var i = 2; i < 10; i++)
            {
                await cachedEcho(key);
                
                Assert.Equal(i, results.Count);
                Assert.True(results.Last().Success);
                results.Last().Hits.Should().ContainSingle();
                Assert.Empty(results.Last().Misses);
                Assert.Equal(key, results.Last().Hits.Single());
                Assert.Equal("memory", results.Last().CacheType);
            }
        }
        
        [Fact]
        public async Task DefaultOnCacheSetIsTriggered()
        {
            var results = new List<CacheSetResult>();

            var key = Guid.NewGuid().ToString();

            Func<string, Task<string>> echo = new Echo();
            Func<string, Task<string>> cachedEcho;
            using (_setupLock.Enter(true))
            {
                DefaultSettings.Cache.OnCacheSet(x =>
                {
                    if (x.Keys.Contains(key))
                        results.Add(x);
                });

                cachedEcho = echo
                    .Cached()
                    .Build();

                DefaultSettings.Cache.OnCacheSet(null, AdditionBehaviour.Overwrite);
            }

            await cachedEcho(key);

            results.Should().ContainSingle();
            Assert.True(results.Single().Success);
            results.Single().Keys.Should().ContainSingle();
            Assert.Equal(key, results.Single().Keys.Single());
            Assert.Equal("memory", results.Single().CacheType);
            
            await cachedEcho(key);
            
            results.Should().ContainSingle();
        }

        [Fact]
        public async Task DefaultOnCacheExceptionIsTriggered()
        {
            var errors = new List<CacheException>();

            var key = Guid.NewGuid().ToString();

            Func<string, Task<string>> echo = new Echo();
            Func<string, Task<string>> cachedEcho;
            using (_setupLock.Enter(true))
            {
                DefaultSettings.Cache.OnCacheException(x =>
                {
                    if (x.Keys.Contains(key))
                        errors.Add(x);
                });

                cachedEcho = echo
                    .Cached()
                    .WithDistributedCache(new TestCache<string, string>(x => x, x => x, error: () => true))
                    .Build();

                DefaultSettings.Cache.OnException(null, AdditionBehaviour.Overwrite);
            }

            await Assert.ThrowsAnyAsync<FunctionCacheException>(() => cachedEcho(key));

            errors.Should().ContainSingle();
            errors.Single().Keys.Should().ContainSingle();
            Assert.Equal(key, errors.Single().Keys.Single());
            Assert.Equal("test", errors.Single().CacheType);
        }
        
        [Fact]
        public async Task DefaultValueByteSerializer()
        {
            var serializer = new TestByteSerializer();
            
            Func<string, Task<string>> echo = new Echo();
            Func<string, Task<string>> cachedEcho;
            using (_setupLock.Enter(true))
            {
                DefaultSettings.Cache.ValueSerializers.SetDefault(serializer);

                cachedEcho = echo
                    .Cached()
                    .WithDistributedCacheFactory(new TestCacheFactory())
                    .Build();

                DefaultSettings.Cache.ValueSerializers.SetDefaultFactory(default(Func<Type, IByteSerializer>));
            }

            await cachedEcho("123");

            serializer.SerializeCount.Should().Be(1);
        }
        
        [Fact]
        public async Task OnlyStoreNegativesInLocalCache()
        {
            var localCache = new TestLocalCache<int, int>();
            var distributedCache = new TestCache<int, int>(x => x.ToString(), Int32.Parse);

            Func<IEnumerable<int>, Task<Dictionary<int, int>>> func = keys => Task.FromResult(keys.Where(k => k % 2 == 1).ToDictionary(k => k));
            Func<IEnumerable<int>, Task<Dictionary<int, int>>> cachedFunc;
            using (_setupLock.Enter(true))
            {
                DefaultSettings.Cache.OnlyStoreNegativesInLocalCache();
                
                cachedFunc = func
                    .Cached<IEnumerable<int>, Dictionary<int, int>, int, int>()
                    .WithLocalCache(localCache)
                    .WithDistributedCache(distributedCache)
                    .WithNegativeCaching()
                    .Build();
                
                DefaultSettings.Cache.OnlyStoreNegativesInLocalCache(false);
            }

            var allKeys = Enumerable.Range(0, 10).ToArray();

            await cachedFunc(allKeys);

            localCache.Values.Should().ContainKeys(0, 2, 4, 6, 8);
            localCache.Values.Values.Select(v => v.Item1).Should().OnlyContain(v => v == 0);
            distributedCache.Values.Should().ContainKeys(allKeys.Select(k => k.ToString()));
        }
        
        private static string GetRandomKey()
        {
            return KeyPrefix + Guid.NewGuid();
        }
    }
}