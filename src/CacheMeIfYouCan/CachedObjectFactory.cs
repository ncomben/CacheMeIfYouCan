﻿using System;
using System.Threading.Tasks;
using CacheMeIfYouCan.Configuration;

namespace CacheMeIfYouCan
{
    public static class CachedObjectFactory
    {
        public static CachedObjectConfig<T> ConfigureFor<T>(Func<T> getValueFunc)
        {
            return ConfigureFor(() => Task.FromResult(getValueFunc()));
        }

        public static CachedObjectConfig<T> ConfigureFor<T>(Func<Task<T>> getValueFunc)
        {
            return new CachedObjectConfig<T>(getValueFunc);
        }
    }
}