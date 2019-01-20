using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CacheMeIfYouCan.Notifications;
using FluentAssertions;
using Xunit;

namespace CacheMeIfYouCan.Tests.CachedObject
{
    [Collection(TestCollections.CachedObject)]
    public class General
    {
        private readonly CachedObjectSetupLock _setupLock;

        public General(CachedObjectSetupLock setupLock)
        {
            _setupLock = setupLock;
        }
        
        [Fact]
        public async Task RefreshedValueIsImmediatelyExposed()
        {
            var results = new List<CachedObjectRefreshResult>();

            ICachedObject<DateTime> date;
            using (_setupLock.Enter())
            {
                date = CachedObjectFactory
                    .ConfigureFor(() => DateTime.UtcNow)
                    .WithRefreshInterval(TimeSpan.FromSeconds(1))
                    .OnRefresh(r =>
                    {
                        results.Add(r);
                        Assert.InRange(
                            r.NewValue,
                            DateTime.UtcNow.AddMilliseconds(-100),
                            DateTime.UtcNow.AddMilliseconds(100));
                    })
                    .Build();
            }

            await date.Initialize();

            await Task.Delay(TimeSpan.FromSeconds(10));
            
            date.Dispose();
            
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task ContinuesToRefreshAfterException()
        {
            var index = 0;
            var refreshResults = new List<CachedObjectRefreshResult>();

            ICachedObject<DateTime> date;
            using (_setupLock.Enter())
            {
                date = CachedObjectFactory
                    .ConfigureFor(() =>
                    {
                        if (index++ == 1)
                            throw new Exception();

                        return DateTime.UtcNow;
                    })
                    .WithRefreshInterval(TimeSpan.FromMilliseconds(200))
                    .OnRefresh(refreshResults.Add)
                    .Build();
            }

            await date.Initialize();
            
            await Task.Delay(TimeSpan.FromSeconds(5));
            
            date.Dispose();
            
            refreshResults.Count.Should().BeGreaterThan(2);

            for (var i = 0; i < refreshResults.Count; i++)
            {
                var result = refreshResults[i];
                
                if (i == 1)
                    result.Success.Should().BeFalse();
                else
                    result.Success.Should().BeTrue();
            }
        }

        [Fact]
        public async Task ThrowsIfAccessedAfterDisposed()
        {
            ICachedObject<DateTime> date;
            using (_setupLock.Enter())
            {
                date = CachedObjectFactory
                    .ConfigureFor(() => DateTime.UtcNow)
                    .WithRefreshInterval(TimeSpan.FromSeconds(1))
                    .Build();
            }

            await date.Initialize();

            date.Value.Should().BeAfter(DateTime.MinValue);
            
            date.Dispose();

            Func<DateTime> act = () => date.Value;
            act.Should().Throw<ObjectDisposedException>();
        }
    }
}