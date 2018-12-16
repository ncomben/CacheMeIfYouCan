using System;
using System.Collections.Generic;

namespace CacheMeIfYouCan.Notifications
{
    public abstract class CacheException<TK> : CacheException
    {
        internal CacheException(
            string cacheName,
            string cacheType,
            Lazy<ICollection<string>> keysAsStrings,
            long timestamp,
            string message,
            Exception exception)
            : base(cacheName, cacheType, keysAsStrings, timestamp, message, exception)
        { }
        
        public new abstract ICollection<Key<TK>> Keys { get; }
    }
    
    public abstract class CacheException : Exception
    {
        private readonly Lazy<ICollection<string>> _keys;

        internal CacheException(
            string cacheName,
            string cacheType,
            Lazy<ICollection<string>> keys,
            long timestamp,
            string message,
            Exception exception)
            : base(message, exception)
        {
            CacheName = cacheName;
            CacheType = cacheType;
            Timestamp = timestamp;
            _keys = keys;
        }

        public string CacheName { get; }
        public string CacheType { get; }
        public long Timestamp { get; }
        public ICollection<string> Keys => _keys.Value;
    }
}