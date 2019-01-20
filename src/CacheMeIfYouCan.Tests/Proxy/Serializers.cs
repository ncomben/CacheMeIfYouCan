﻿using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CacheMeIfYouCan.Tests.Proxy
{
    [Collection(TestCollections.Proxy)]
    public class Serializers
    {
        private readonly CacheSetupLock _setupLock;

        public Serializers(CacheSetupLock setupLock)
        {
            _setupLock = setupLock;
        }
        
        [Fact]
        public async Task SetKeySerializer()
        {
            var serializerA = new TestSerializer();
            var serializerB = new TestSerializer();

            ITest impl = new TestImpl();
            ITest proxy;
            using (_setupLock.Enter())
            {
                proxy = impl
                    .Cached()
                    .WithDistributedCacheFactory(new TestCacheFactory())
                    .WithKeySerializers(c => c
                        .SetDefault(serializerA)
                        .Set<string>(serializerB))
                    .Build();
            }

            await proxy.LongToInt(123);
            serializerA.SerializeCount.Should().Be(1);
            
            serializerA.ResetCounts();
            
            for (var i = 1; i < 10; i++)
            {
                await proxy.LongToInt(123);

                serializerA.SerializeCount.Should().Be(i);
            }
            
            serializerA.ResetCounts();
            
            serializerB.SerializeCount.Should().Be(0);
            
            await proxy.StringToString("abc");
            serializerB.SerializeCount.Should().Be(1);
            
            serializerB.ResetCounts();
            
            for (var i = 1; i < 10; i++)
            {
                await proxy.StringToString("abc");

                serializerB.SerializeCount.Should().Be(i);
            }
            
            serializerA.SerializeCount.Should().Be(0);
        }
        
        [Fact]
        public async Task SetValueSerializer()
        {
            var serializerA = new TestSerializer();
            var serializerB = new TestSerializer();

            ITest impl = new TestImpl();
            ITest proxy;
            using (_setupLock.Enter())
            {
                proxy = impl
                    .Cached()
                    .WithDistributedCacheFactory(new TestCacheFactory())
                    .WithValueSerializers(c => c
                        .SetDefault(serializerA)
                        .Set<string>(serializerB))
                    .Build();
            }

            await proxy.LongToInt(123);
            serializerA.SerializeCount.Should().Be(1);
            
            serializerA.ResetCounts();
            
            for (var i = 1; i < 10; i++)
            {
                await proxy.LongToInt(123);

                serializerA.SerializeCount.Should().Be(0);
                serializerA.DeserializeCount.Should().Be(i);
            }
            
            serializerA.ResetCounts();
            
            serializerB.SerializeCount.Should().Be(0);
            serializerB.DeserializeCount.Should().Be(0);
            
            await proxy.StringToString("abc");
            serializerB.SerializeCount.Should().Be(1);
            
            serializerB.ResetCounts();
            
            for (var i = 1; i < 10; i++)
            {
                await proxy.StringToString("abc");

                serializerB.SerializeCount.Should().Be(0);
                serializerB.DeserializeCount.Should().Be(i);
            }
            
            serializerA.SerializeCount.Should().Be(0);
            serializerA.DeserializeCount.Should().Be(0);
        }
    }
}
