﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheMeIfYouCan.Events.CachedFunction.SingleKey;
using FluentAssertions;
using Xunit;

namespace CacheMeIfYouCan.Tests
{
    public class MappedCachedObjectTests
    {
        [Fact]
        public void ValueIsMappedFromSourceCorrectly()
        {
            var source = CachedObjectFactory
                .ConfigureFor(() => DateTime.UtcNow.Ticks)
                .Build();

            var mapped = source.Map(x => -x);

            mapped.Value.Should().Be(-source.Value);
        }

        [Fact]
        public void ValueIsUpdatedEachTimeSourceIsRefreshed()
        {
            var source = CachedObjectFactory
                .ConfigureFor(() => DateTime.UtcNow.Ticks)
                .Build();

            var mapped = source.Map(x => -x);

            mapped.Initialize();

            for (var i = 0; i < 10; i++)
            {
                source.RefreshValue();
                mapped.Version.Should().Be(i + 2);
                mapped.Value.Should().Be(-source.Value);
            }
        }
        
        [Fact]
        public async Task InitializationIsDeferredUntilRequired()
        {
            var source = CachedObjectFactory
                .ConfigureFor(() => DateTime.UtcNow)
                .Build();

            var mapped = source.Map(x => DateTime.UtcNow);

            DateTime dateOfSourceInitialization = default;
            DateTime dateOfDestinationInitialization = default;

            source.OnInitialized += (_, __) => dateOfSourceInitialization = DateTime.UtcNow;
            mapped.OnInitialized += (_, __) => dateOfDestinationInitialization = DateTime.UtcNow;
            
            await source.InitializeAsync().ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

            await mapped.InitializeAsync().ConfigureAwait(false);

            source.Value.Should().BeCloseTo(dateOfSourceInitialization);
            mapped.Value.Should().BeCloseTo(dateOfDestinationInitialization);
            var diff = mapped.Value - source.Value;
            diff.Should().BeCloseTo(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void MappedCachedObjectsCanBeChained()
        {
            var source = CachedObjectFactory
                .ConfigureFor(() => DateTime.UtcNow)
                .Build();

            var cachedObjects = new ICachedObject<DateTime>[10];

            var previous = source;
            for (var i = 0; i < 10; i++)
            {
                var current = previous.Map(x => x.AddDays(1));
                cachedObjects[i] = current;
                previous = current;
            }

            foreach (var cachedObject in cachedObjects)
                cachedObject.State.Should().Be(CachedObjectState.PendingInitialization);
            
            cachedObjects[9].Initialize();
            
            foreach (var cachedObject in cachedObjects)
                cachedObject.State.Should().Be(CachedObjectState.Ready);
            
            for (var i = 0; i < 10; i++)
                cachedObjects[i].Value.Should().Be(source.Value.AddDays(i + 1));
        }

        [Fact]
        public void IfMappingFunctionThrowsException_LaterUpdatesAreStillProcessed()
        {
            var source = CachedObjectFactory
                .ConfigureFor(() => DateTime.UtcNow)
                .Build();

            var count = 0;

            var mapped = source.Map(x =>
            {
                if (count++ % 2 == 1)
                    throw new Exception();

                return x;
            });

            mapped.Initialize();
            
            for (var i = 0; i < 10; i++)
            {
                source.RefreshValue();

                if (i % 2 == 0)
                    mapped.Value.Should().BeBefore(source.Value);
                else
                    mapped.Value.Should().Be(source.Value);
            }
        }

        [Fact]
        public async Task MultipleConcurrentCallsToInitialize_MappingFunctionCalledOnce()
        {
            var source = CachedObjectFactory
                .ConfigureFor(() => DateTime.UtcNow)
                .Build();

            var executions = 0;
            
            var mapped = source.Map(x =>
            {
                Interlocked.Increment(ref executions);
                Thread.Sleep(TimeSpan.FromSeconds(1));
                return x;
            });

            var tasks = Enumerable
                .Range(0, 10).Select(_ => Task.Run(() => mapped.InitializeAsync()))
                .ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            executions.Should().Be(1);
        }
    }
}