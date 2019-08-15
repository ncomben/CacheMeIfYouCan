using System;
using FluentAssertions;
using Xunit;

namespace CacheMeIfYouCan.Tests.Proxy
{
    [Collection(TestCollections.Proxy)]
    public class WithUnsupportedMethods
    {
        private readonly CacheSetupLock _setupLock;

        public WithUnsupportedMethods(CacheSetupLock setupLock)
        {
            DefaultSettings.Cache.AllowInterfacesWithUnsupportedMethods();
            
            _setupLock = setupLock;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ThrowsIfNotAllowedInDefaultSettings(bool allowed)
        {
            ITestWithUnsupportedMethods impl = new TestImpl();
            using (_setupLock.Enter(true))
            {
                DefaultSettings.Cache.AllowInterfacesWithUnsupportedMethods(allowed);
                
                Func<ITest> func = () => impl
                    .Cached()
                    .Build();

                if (allowed)
                    func.Should().NotThrow();
                else
                    func.Should().Throw<Exception>();

                DefaultSettings.Cache.AllowInterfacesWithUnsupportedMethods();
            }
        }
        
        [Fact]
        public void UnsupportedFuncSucceeds()
        {
            ITestWithUnsupportedMethods impl = new TestImpl();
            ITestWithUnsupportedMethods proxy;
            using (_setupLock.Enter())
            {
                proxy = impl
                    .Cached()
                    .Build();
            }

            proxy.UnsupportedFunc(1, 2, 3, 4, 5).Should().Be("1_2_3_4_5");
        }
        
        [Fact]
        public void UnsupportedActionSucceeds()
        {
            ITestWithUnsupportedMethods impl = new TestImpl();
            ITestWithUnsupportedMethods proxy;
            using (_setupLock.Enter())
            {
                proxy = impl
                    .Cached()
                    .Build();
            }

            proxy.UnsupportedAction(1);
        }
        
        [Fact]
        public void UnsupportedPropertySucceeds()
        {
            ITestWithUnsupportedMethods impl = new TestImpl();
            ITestWithUnsupportedMethods proxy;
            using (_setupLock.Enter())
            {
                proxy = impl
                    .Cached()
                    .Build();
            }

            proxy.UnsupportedProperty = 1;
            proxy.UnsupportedProperty.Should().Be(1);
        }
    }
}