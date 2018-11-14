﻿using System;
using CacheMeIfYouCan.Configuration;

namespace CacheMeIfYouCan
{
    public static class InterfaceExtensions
    {
        public static CachedProxyConfigurationManager<T> Cached<T>(this T impl) where T : class
        {
            if (!typeof(T).IsInterface)
                throw new Exception($"Type '{typeof(T).FullName}' is not an interface");
            
            return new CachedProxyConfigurationManager<T>(impl);
        }
    }
}