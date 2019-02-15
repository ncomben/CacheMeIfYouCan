using CacheMeIfYouCan.Configuration;
using CacheMeIfYouCan.Tests.Common;
using FluentAssertions;

namespace CacheMeIfYouCan.Tests.Helpers
{
    public class KeyspacePrefixCheckingCacheFactory : IDistributedCacheFactory
    {
        public IDistributedCache<TK, TV> Build<TK, TV>(DistributedCacheConfig<TK, TV> config)
        {
            config.KeyspacePrefix.Should().Be("prefix");

            BuildCount++;
                
            return new TestCache<TK, TV>();
        }
            
        public int BuildCount { get; private set; }
    }
}