﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheMeIfYouCan.Notifications;
using FluentAssertions;
using Xunit;

namespace CacheMeIfYouCan.Tests.CachedObject
{
    [Collection(TestCollections.CachedObject)]
    public class RefreshInterval
    {
        private readonly CachedObjectSetupLock _setupLock;

        public RefreshInterval(CachedObjectSetupLock setupLock)
        {
            _setupLock = setupLock;
        }
        
        [Fact]
        public async Task ValueIsRefreshedAtRegularIntervals()
        {
            var refreshResults = new List<CachedObjectRefreshResult>();
            var countdown = new CountdownEvent(4);

            ICachedObject<DateTime> date;
            using (_setupLock.Enter())
            {
                date = CachedObjectFactory
                    .ConfigureFor(() => DateTime.UtcNow)
                    .WithRefreshInterval(TimeSpan.FromSeconds(4))
                    .OnRefresh(r =>
                    {
                        refreshResults.Add(r);
                        countdown.Signal();
                    })
                    .Build();
            }

            await date.Initialize();

            countdown.Wait(TimeSpan.FromMinutes(1));

            date.Dispose();
            
            var min = refreshResults.Skip(1).Select(r => r.Start - r.LastRefreshAttempt).Min();
            var max = refreshResults.Skip(1).Select(r => r.Start - r.LastRefreshAttempt).Max();

            min.Should().BeGreaterThan(TimeSpan.FromMilliseconds(3800));
            max.Should().BeLessThan(TimeSpan.FromMilliseconds(6000));
        }
        
        [Fact]
        public async Task JitterCausesRefreshIntervalsToFluctuate()
        {
            var refreshResults = new List<CachedObjectRefreshResult>();
            var countdown = new CountdownEvent(15);
            
            ICachedObject<DateTime> date;
            using (_setupLock.Enter())
            {
                date = CachedObjectFactory
                    .ConfigureFor(() => DateTime.UtcNow)
                    .WithRefreshInterval(TimeSpan.FromSeconds(1))
                    .WithJitterPercentage(50)
                    .OnRefresh(r =>
                    {
                        refreshResults.Add(r);
                        countdown.Signal();
                    })
                    .Build();
            }

            await date.Initialize();

            countdown.Wait(TimeSpan.FromMinutes(1));
            
            date.Dispose();
            
            var min = refreshResults.Skip(1).Select(r => r.Start - r.LastRefreshAttempt).Min();
            var max = refreshResults.Skip(1).Select(r => r.Start - r.LastRefreshAttempt).Max();
            
            min.Should().BeGreaterThan(TimeSpan.FromMilliseconds(500)).And.BeLessThan(TimeSpan.FromMilliseconds(900));
            max.Should().BeGreaterThan(TimeSpan.FromMilliseconds(1200));
        }

        [Fact]
        public async Task VariableRefreshInterval()
        {
            var refreshResults = new List<CachedObjectRefreshResult>();
            var countdown = new CountdownEvent(5);
            var refreshCount = 0;
            
            ICachedObject<DateTime> date;
            using (_setupLock.Enter())
            {
                date = CachedObjectFactory
                    .ConfigureFor(() => DateTime.UtcNow)
                    .WithRefreshInterval(() => TimeSpan.FromSeconds(++refreshCount))
                    .OnRefresh(r =>
                    {
                        refreshResults.Add(r);
                        countdown.Signal();
                    })
                    .Build();
            }

            await date.Initialize();

            countdown.Wait(TimeSpan.FromMinutes(1));
            
            date.Dispose();
            
            refreshResults.Count.Should().Be(5);
            
            foreach (var result in refreshResults.Skip(1))
            {
                var interval = result.Start - result.LastRefreshAttempt;
                    
                interval
                    .Should()
                    .BeGreaterThan(TimeSpan.FromSeconds(result.SuccessfulRefreshCount - 1.1))
                    .And
                    .BeLessThan(TimeSpan.FromSeconds(result.SuccessfulRefreshCount + 2));
            }
        }
        
        [Fact]
        public async Task OnSuccessAndOnFailureRefreshIntervals()
        {
            var refreshResults = new List<CachedObjectRefreshResult>();
            var countdown = new CountdownEvent(5);
            
            ICachedObject<DateTime> date;
            using (_setupLock.Enter())
            {
                date = CachedObjectFactory
                    .ConfigureFor(() => refreshResults.Count % 2 == 0 ? DateTime.UtcNow : throw new Exception())
                    .WithRefreshInterval(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3))
                    .OnRefresh(r =>
                    {
                        refreshResults.Add(r);
                        countdown.Signal();
                    })
                    .Build();
            }

            await date.Initialize();

            countdown.Wait(TimeSpan.FromMinutes(1));
            
            date.Dispose();
            
            refreshResults.Count.Should().Be(5);
            
            for (var i = 1; i < 5; i++)
            {
                var result = refreshResults[i];
                
                var interval = result.Start - result.LastRefreshAttempt;

                TimeSpan min;
                TimeSpan max;
                if (i % 2 == 1)
                {
                    min = TimeSpan.FromSeconds(0.9);
                    max = TimeSpan.FromSeconds(2);
                }
                else
                {
                    min = TimeSpan.FromSeconds(2.9);
                    max = TimeSpan.FromSeconds(4);
                }
                
                interval
                    .Should()
                    .BeGreaterThan(min)
                    .And
                    .BeLessThan(max);
            }
        }
    }
}