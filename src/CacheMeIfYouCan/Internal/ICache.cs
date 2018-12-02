using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CacheMeIfYouCan.Internal
{
    public interface ICache<TK, TV>
    {
        string CacheName { get; }
        string CacheType { get; }
        ValueTask<GetFromCacheResult<TK, TV>> Get(Key<TK> key);
        ValueTask Set(Key<TK> key, TV value, TimeSpan timeToLive);
        ValueTask<IList<GetFromCacheResult<TK, TV>>> Get(ICollection<Key<TK>> keys);
        ValueTask Set(ICollection<KeyValuePair<Key<TK>, TV>> values, TimeSpan timeToLive);
    }
}