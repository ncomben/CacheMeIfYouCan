﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CacheMeIfYouCan.Internal
{
    internal sealed class CachedFunctionConfiguration<TKey, TValue>
    {
        public CachedFunctionConfiguration(Func<TKey, CancellationToken, Task<TValue>> originalFunc)
        {
            OriginalFunction = originalFunc;
        }
        
        public Func<TKey, CancellationToken, Task<TValue>> OriginalFunction { get; }
        public Func<TKey, TimeSpan> TimeToLiveFactory { get; set; }
        public ILocalCache<TKey, TValue> LocalCache { get; set; }
        public IDistributedCache<TKey, TValue> DistributedCache { get; set; }
        public IEqualityComparer<TKey> KeyComparer { get; set; }
    }
}