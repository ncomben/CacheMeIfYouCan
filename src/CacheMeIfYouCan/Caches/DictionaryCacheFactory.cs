﻿namespace CacheMeIfYouCan.Caches
{
    public class DictionaryCacheFactory : ILocalCacheFactory
    {
        public bool RequiresStringKeys => false;
        
        public ILocalCache<TK, TV> Build<TK, TV>()
        {
            return new DictionaryCache<TK, TV>();
        }
    }
}