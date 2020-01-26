﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;

namespace CacheMeIfYouCan.Redis.Tests
{
    /// <summary>
    /// Tests for <see cref="RedisCache{TOuterKey,TInnerKey,TValue}"/>
    /// </summary>
    public class RedisCacheTests2
    {
        [Fact]
        public void Concurrent_SetMany_GetMany_AllItemsReturnedSuccessfully()
        {
            var cache = BuildRedisCache();

            var tasks = Enumerable
                .Range(1, 5)
                .Select(i => Task.Run(async () =>
                {
                    for (var j = 0; j < 100; j++)
                    {
                        var keys = Enumerable.Range((10 * i) + j, i).ToList();
                        await cache.SetMany(1, keys.Select(k => new KeyValuePair<int, int>(k, k)).ToList(), TimeSpan.FromSeconds(1));
                        var values = await cache.GetMany(1, keys);
                        values.Select(kv => kv.Key).Should().BeEquivalentTo(keys);
                        values.Select(kv => kv.Value.Value).Should().BeEquivalentTo(keys);
                        foreach (var timeToLive in values.Select(kv => kv.Value.TimeToLive))
                            timeToLive.Should().BePositive().And.BeLessThan(TimeSpan.FromSeconds(1));
                        
                        Thread.Yield();
                    }
                }))
                .ToArray();

            Task.WaitAll(tasks);
        }
        
        [Fact]
        public async Task GetMany_WithSomeKeysNotSet_ReturnsAllKeysWhichHaveValues()
        {
            var cache = BuildRedisCache();

            var keys = Enumerable.Range(1, 10).ToList();
            var values = keys.Where(k => k % 2 == 0).Select(i => new KeyValuePair<int, int>(i, i)).ToList();

            await cache.SetMany(1, values, TimeSpan.FromSeconds(1));

            var fromCache = await cache.GetMany(1, keys);

            fromCache.Select(kv => kv.Key).Should().BeEquivalentTo(values.Select(kv => kv.Key));
        }
        
        [Theory]
        [InlineData(50)]
        [InlineData(1000)]
        public async Task WithTimeToLive_DataExpiredCorrectly(int timeToLiveMs)
        {
            var cache = BuildRedisCache();
            
            await cache.SetMany(1, new[] { new KeyValuePair<int, int>(1, 1) }, TimeSpan.FromMilliseconds(timeToLiveMs));
            (await cache.GetMany(1, new[] { 1 })).Should().ContainSingle();
            
            Thread.Sleep(timeToLiveMs + 20);

            (await cache.GetMany(1, new[] { 1 })).Should().BeEmpty();
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WhenFireAndForgetEnabled_SetOperationsCompleteImmediately(bool useFireAndForget)
        {
            var cache = BuildRedisCache(useFireAndForget);

            var task = cache.SetMany(1, new[] { new KeyValuePair<int, int>(1, 1) }, TimeSpan.FromSeconds(1));

            task.IsCompleted.Should().Be(useFireAndForget);
        }
        
        [Fact]
        public async Task WhenDisposed_ThrowsObjectDisposedException()
        {
            var cache = BuildRedisCache();
            
            cache.Dispose();

            Func<Task> task = () => cache.GetMany(1, new[] { 1 });

            await task.Should().ThrowExactlyAsync<ObjectDisposedException>().WithMessage($"* '{cache.GetType()}'.");
        }

        private static RedisCache<int, int, int> BuildRedisCache(
            bool useFireAndForget = false,
            string keyPrefix = null,
            string nullValue = null)
        {
            var connectionMultiplexer = ConnectionMultiplexer.Connect(TestConnectionString.Value);
            
            return new RedisCache<int, int, int>(
                connectionMultiplexer,
                k => k.ToString(),
                k => k.ToString(),
                v => (RedisValue)v,
                v => (int)v,
                useFireAndForgetWherePossible: useFireAndForget,
                keyPrefix: keyPrefix ?? Guid.NewGuid().ToString(),
                nullValue: nullValue);
        }
    }
}