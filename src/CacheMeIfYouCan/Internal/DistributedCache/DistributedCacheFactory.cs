using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheMeIfYouCan.Configuration;
using CacheMeIfYouCan.Notifications;
using CacheMeIfYouCan.Serializers;

namespace CacheMeIfYouCan.Internal.DistributedCache
{
    internal class DistributedCacheFactory : IDistributedCacheFactory
    {
        private readonly IDistributedCacheFactory _cacheFactory;
        private readonly KeySerializers _keySerializers;
        private readonly ValueSerializers _valueSerializers;
        private readonly EqualityComparers _keyComparers;
        private readonly List<IDistributedCacheWrapperFactory> _wrapperFactories;
        private Action<CacheGetResult> _onGetResult;
        private Action<CacheSetResult> _onSetResult;
        private Action<CacheRemoveResult> _onRemoveResult;
        private Action<CacheException> _onException;
        private string _keyspacePrefix;
        private Func<Exception, bool> _swallowExceptionsPredicate;

        public DistributedCacheFactory(IDistributedCacheFactory cacheFactory)
        {
            _cacheFactory = cacheFactory;
            _keySerializers = new KeySerializers();
            _valueSerializers = new ValueSerializers();
            _keyComparers = new EqualityComparers();
            _wrapperFactories = new List<IDistributedCacheWrapperFactory>();
        }

        public DistributedCacheFactory OnGetResult(
            Action<CacheGetResult> onGetResult,
            AdditionBehaviour behaviour = AdditionBehaviour.Append)
        {
            _onGetResult = ActionsHelper.Combine(_onGetResult, onGetResult, behaviour);
            return this;
        }

        public DistributedCacheFactory OnSetResult(
            Action<CacheSetResult> onSetResult,
            AdditionBehaviour behaviour = AdditionBehaviour.Append)
        {
            _onSetResult = ActionsHelper.Combine(_onSetResult, onSetResult, behaviour);
            return this;
        }

        public DistributedCacheFactory OnRemoveResult(
            Action<CacheRemoveResult> onRemoveResult,
            AdditionBehaviour behaviour = AdditionBehaviour.Append)
        {
            _onRemoveResult = ActionsHelper.Combine(_onRemoveResult, onRemoveResult, behaviour);
            return this;
        }
        
        public DistributedCacheFactory OnException(
            Action<CacheException> onException,
            AdditionBehaviour behaviour = AdditionBehaviour.Append)
        {
            _onException = ActionsHelper.Combine(_onException, onException, behaviour);
            return this;
        }
        
        public DistributedCacheFactory WithKeySerializers(Action<KeySerializers> configAction)
        {
            configAction(_keySerializers);
            return this;
        }
        
        public DistributedCacheFactory WithValueSerializers(Action<ValueSerializers> configAction)
        {
            configAction(_valueSerializers);
            return this;
        }

        public DistributedCacheFactory WithKeyComparer<T>(IEqualityComparer<T> comparer)
        {
            _keyComparers.Set(comparer);
            return this;
        }
        
        public DistributedCacheFactory WithKeyspacePrefix(string keyspacePrefix)
        {
            _keyspacePrefix = keyspacePrefix;
            return this;
        }
        
        public DistributedCacheFactory WithWrapper(
            IDistributedCacheWrapperFactory wrapperFactory,
            AdditionBehaviour behaviour)
        {
            switch (behaviour)
            {
                case AdditionBehaviour.Append:
                    _wrapperFactories.Add(wrapperFactory);
                    break;

                case AdditionBehaviour.Prepend:
                    _wrapperFactories.Insert(0, wrapperFactory);
                    break;
                
                case AdditionBehaviour.Overwrite:
                    _wrapperFactories.Clear();
                    _wrapperFactories.Add(wrapperFactory);
                    break;
            }
            
            return this;
        }
        
        public DistributedCacheFactory SwallowExceptions(Func<Exception, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            var current = _swallowExceptionsPredicate;

            if (current == null)
                _swallowExceptionsPredicate = predicate;
            else
                _swallowExceptionsPredicate = ex => current(ex) || predicate(ex);

            return this;
        }

        public IDistributedCache<TK, TV> Build<TK, TV>(DistributedCacheConfig<TK, TV> config)
        {
            var original = config;

            config = new DistributedCacheConfig<TK, TV>(original.CacheName, true);

            if (original.KeyspacePrefix != null)
                config.KeyspacePrefix = original.KeyspacePrefix;
            else if (_keyspacePrefix != null)
                config.KeyspacePrefix = _keyspacePrefix;

            if (original.KeySerializer != null)
                config.KeySerializer = original.KeySerializer;
            else if (_keySerializers.TryGetSerializer<TK>(out var keySerializer))
                config.KeySerializer = keySerializer;

            if (original.KeyDeserializer != null)
                config.KeyDeserializer = original.KeyDeserializer;
            else if (_keySerializers.TryGetDeserializer<TK>(out var keyDeserializer))
                config.KeyDeserializer = keyDeserializer;

            if (original.ValueSerializer != null)
                config.ValueSerializer = original.ValueSerializer;
            else if (original.ValueByteSerializer != null)
                config.ValueByteSerializer = original.ValueByteSerializer;
            else if (_valueSerializers.TryGetSerializer<TV>(out var valueSerializer))
                config.ValueSerializer = valueSerializer;
            else if (_valueSerializers.TryGetByteSerializer<TV>(out var valueByteSerializer))
                config.ValueByteSerializer = valueByteSerializer;

            if (original.ValueDeserializer != null)
                config.ValueDeserializer = original.ValueDeserializer;
            else if (original.ValueByteDeserializer != null)
                config.ValueByteDeserializer = original.ValueByteDeserializer;
            else if (_valueSerializers.TryGetDeserializer<TV>(out var valueDeserializer))
                config.ValueDeserializer = valueDeserializer;
            else if (_valueSerializers.TryGetByteDeserializer<TV>(out var valueByteDeserializer))
                config.ValueByteDeserializer = valueByteDeserializer;

            if (original.KeyComparer != null)
                config.KeyComparer = original.KeyComparer;
            else if (_keyComparers.TryGet<TK>(out var comparer))
                config.KeyComparer = new KeyComparer<TK>(comparer);
            
            config.Validate();

            var originalCache = _cacheFactory.Build(config);

            var cache = originalCache;
            
            // First apply any custom wrappers
            foreach (var wrapperFactory in _wrapperFactories)
                cache = wrapperFactory.Wrap(cache, config);
            
            // Then add a wrapper to catch and format any exceptions
            cache = new DistributedCacheExceptionFormattingWrapper<TK, TV>(cache);

            // Then add a wrapper to handle notifications (if any actions are set)
            if (_onGetResult != null || _onSetResult != null || _onRemoveResult != null || _onException != null)
                cache = new DistributedCacheNotificationWrapper<TK, TV>(cache, _onGetResult, _onSetResult, _onRemoveResult, _onException);

            // Then add a wrapper to swallow exceptions (if required)
            if (_swallowExceptionsPredicate != null)
                cache = new DistributedCacheExceptionSwallowingWrapper<TK, TV>(cache, _swallowExceptionsPredicate);

            return new WrappedDistributedCacheWithOriginal<TK, TV>(cache, originalCache);
        }

        public IDistributedCache<TK, TV> Build<TK, TV>(string cacheName)
        {
            var config = new DistributedCacheConfig<TK, TV>(cacheName);
            
            return Build(config);
        }
        
        public ICache<TK, TV> BuildAsCache<TK, TV>(string cacheName)
        {
            var cache = Build<TK, TV>(cacheName);
            
            _keySerializers.TryGetSerializer<TK>(out var keySerializer);
            
            return new DistributedCacheToCacheAdapter<TK, TV>(cache, keySerializer);
        }
    }
    
    internal class DistributedCacheFactory<TK, TV> : IDistributedCacheFactory<TK, TV>
    {
        private readonly IDistributedCacheFactory<TK, TV> _cacheFactory;
        private readonly List<IDistributedCacheWrapperFactory<TK, TV>> _wrapperFactories;
        private Action<CacheGetResult<TK, TV>> _onGetResult;
        private Action<CacheSetResult<TK, TV>> _onSetResult;
        private Action<CacheRemoveResult<TK>> _onRemoveResult;
        private Action<CacheException<TK>> _onException;
        private Func<TK, string> _keySerializer;
        private Func<string, TK> _keyDeserializer;
        private Func<TV, string> _valueSerializer;
        private Func<string, TV> _valueDeserializer;
        private Func<TV, byte[]> _valueByteSerializer;
        private Func<byte[], TV> _valueByteDeserializer;
        private KeyComparer<TK> _keyComparer;
        private string _keyspacePrefix;
        private Func<Exception, bool> _swallowExceptionsPredicate;
        
        internal DistributedCacheFactory(IDistributedCacheFactory<TK, TV> cacheFactory)
        {
            _cacheFactory = cacheFactory ?? throw new ArgumentNullException(nameof(cacheFactory));
            _wrapperFactories = new List<IDistributedCacheWrapperFactory<TK, TV>>();
        }

        public DistributedCacheFactory<TK, TV> OnGetResult(
            Action<CacheGetResult<TK, TV>> onGetResult,
            AdditionBehaviour behaviour = AdditionBehaviour.Append)
        {
            _onGetResult = ActionsHelper.Combine(_onGetResult, onGetResult, behaviour);
            return this;
        }

        public DistributedCacheFactory<TK, TV> OnSetResult(
            Action<CacheSetResult<TK, TV>> onSetResult,
            AdditionBehaviour behaviour = AdditionBehaviour.Append)
        {
            _onSetResult = ActionsHelper.Combine(_onSetResult, onSetResult, behaviour);
            return this;
        }
        
        public DistributedCacheFactory<TK, TV> OnRemoveResult(
            Action<CacheRemoveResult<TK>> onRemoveResult,
            AdditionBehaviour behaviour = AdditionBehaviour.Append)
        {
            _onRemoveResult = ActionsHelper.Combine(_onRemoveResult, onRemoveResult, behaviour);
            return this;
        }

        public DistributedCacheFactory<TK, TV> OnException(
            Action<CacheException<TK>> onException,
            AdditionBehaviour behaviour = AdditionBehaviour.Append)
        {
            _onException = ActionsHelper.Combine(_onException, onException, behaviour);
            return this;
        }

        public DistributedCacheFactory<TK, TV> WithKeySerializer(ISerializer<TK> serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize);
        }

        public DistributedCacheFactory<TK, TV> WithKeySerializer(ISerializer serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize<TK>);
        }

        public DistributedCacheFactory<TK, TV> WithKeySerializer(
            Func<TK, string> serializer,
            Func<string, TK> deserializer)
        {
            _keySerializer = serializer;
            _keyDeserializer = deserializer;
            return this;
        }

        public DistributedCacheFactory<TK, TV> WithValueSerializer(ISerializer<TV> serializer)
        {
            return WithValueSerializer(serializer.Serialize, serializer.Deserialize);
        }

        public DistributedCacheFactory<TK, TV> WithValueSerializer(ISerializer serializer)
        {
            return WithValueSerializer(serializer.Serialize, serializer.Deserialize<TV>);
        }

        public DistributedCacheFactory<TK, TV> WithValueSerializer(
            Func<TV, string> serializer,
            Func<string, TV> deserializer)
        {
            _valueSerializer = serializer;
            _valueDeserializer = deserializer;
            _valueByteSerializer = null;
            _valueByteDeserializer = null;
            return this;
        }
        
        public DistributedCacheFactory<TK, TV> WithValueSerializer(IByteSerializer<TV> serializer)
        {
            return WithValueSerializer(serializer.Serialize, serializer.Deserialize);
        }

        public DistributedCacheFactory<TK, TV> WithValueSerializer(IByteSerializer serializer)
        {
            return WithValueSerializer(serializer.Serialize, serializer.Deserialize<TV>);
        }

        public DistributedCacheFactory<TK, TV> WithValueSerializer(
            Func<TV, byte[]> serializer,
            Func<byte[], TV> deserializer)
        {
            _valueByteSerializer = serializer;
            _valueByteDeserializer = deserializer;
            _valueSerializer = null;
            _valueDeserializer = null;
            return this;
        }

        public DistributedCacheFactory<TK, TV> WithKeyComparer(IEqualityComparer<TK> comparer)
        {
            _keyComparer = new KeyComparer<TK>(comparer);
            return this;
        }
        
        public DistributedCacheFactory<TK, TV> WithKeyspacePrefix(string keyspacePrefix)
        {
            _keyspacePrefix = keyspacePrefix;
            return this;
        }

        public DistributedCacheFactory<TK, TV> WithWrapper(
            IDistributedCacheWrapperFactory<TK, TV> wrapperFactory,
            AdditionBehaviour behaviour)
        {
            switch (behaviour)
            {
                case AdditionBehaviour.Append:
                    _wrapperFactories.Add(wrapperFactory);
                    break;

                case AdditionBehaviour.Prepend:
                    _wrapperFactories.Insert(0, wrapperFactory);
                    break;
                
                case AdditionBehaviour.Overwrite:
                    _wrapperFactories.Clear();
                    _wrapperFactories.Add(wrapperFactory);
                    break;
            }
            
            return this;
        }
        
        public DistributedCacheFactory<TK, TV> SwallowExceptions(Func<Exception, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            var current = _swallowExceptionsPredicate;

            if (current == null)
                _swallowExceptionsPredicate = predicate;
            else
                _swallowExceptionsPredicate = ex => current(ex) || predicate(ex);

            return this;
        }

        public IDistributedCache<TK, TV> Build(DistributedCacheConfig<TK, TV> config)
        {
            var original = config;

            config = new DistributedCacheConfig<TK, TV>(original.CacheName, true);

            if (original.KeyspacePrefix != null)
                config.KeyspacePrefix = original.KeyspacePrefix;
            else if (_keyspacePrefix != null)
                config.KeyspacePrefix = _keyspacePrefix;

            if (original.KeySerializer != null)
                config.KeySerializer = original.KeySerializer;
            else if (_keySerializer != null)
                config.KeySerializer = _keySerializer;

            if (original.KeyDeserializer != null)
                config.KeyDeserializer = original.KeyDeserializer;
            else if (_keyDeserializer != null)
                config.KeyDeserializer = _keyDeserializer;

            if (original.ValueSerializer != null)
                config.ValueSerializer = original.ValueSerializer;
            else if (original.ValueByteSerializer != null)
                config.ValueByteSerializer = original.ValueByteSerializer;
            else if (_valueSerializer != null)
                config.ValueSerializer = _valueSerializer;
            else if (_valueByteSerializer != null)
                config.ValueByteSerializer = _valueByteSerializer;
            
            if (original.ValueDeserializer != null)
                config.ValueDeserializer = original.ValueDeserializer;
            else if (original.ValueByteDeserializer != null)
                config.ValueByteDeserializer = original.ValueByteDeserializer;
            else if (_valueDeserializer != null)
                config.ValueDeserializer = _valueDeserializer;
            else if (_valueByteDeserializer != null)
                config.ValueByteDeserializer = _valueByteDeserializer;

            if (original.KeyComparer != null)
                config.KeyComparer = original.KeyComparer;
            else if (_keyComparer != null)
                config.KeyComparer = new KeyComparer<TK>(_keyComparer);

            config.Validate();
            
            var originalCache = _cacheFactory.Build(config);

            var cache = originalCache;
            
            // First apply any custom wrappers
            foreach (var wrapperFactory in _wrapperFactories)
                cache = wrapperFactory.Wrap(cache, config);
            
            // Then add a wrapper to catch and format any exceptions
            cache = new DistributedCacheExceptionFormattingWrapper<TK, TV>(cache);

            // Then add a wrapper to handle notifications (if any actions are set)
            if (_onGetResult != null || _onSetResult != null || _onRemoveResult != null || _onException != null)
                cache = new DistributedCacheNotificationWrapper<TK, TV>(cache, _onGetResult, _onSetResult, _onRemoveResult, _onException);

            // Then add a wrapper to swallow exceptions (if required)
            if (_swallowExceptionsPredicate != null)
                cache = new DistributedCacheExceptionSwallowingWrapper<TK, TV>(cache, _swallowExceptionsPredicate);

            return new WrappedDistributedCacheWithOriginal<TK, TV>(cache, originalCache);
        }

        internal IDistributedCache<TK, TV> Build(string cacheName)
        {
            var config = new DistributedCacheConfig<TK, TV>(cacheName);
            
            return Build(config);
        }
        
        internal ICache<TK, TV> BuildAsCache(string cacheName)
        {
            var cache = Build(cacheName);
            
            return new DistributedCacheToCacheAdapter<TK, TV>(cache, _keySerializer);
        }
    }

    internal class DistributedCacheToCacheAdapter<TK, TV> : ICache<TK, TV>
    {
        private readonly IDistributedCache<TK, TV> _cache;
        private readonly Func<TK, string> _keySerializer;

        public DistributedCacheToCacheAdapter(IDistributedCache<TK, TV> cache, Func<TK, string> keySerializer)
        {
            _cache = cache;
            
            if (keySerializer != null)
            {
                _keySerializer = keySerializer;
            }
            else if (
                DefaultSettings.Cache.KeySerializers.TryGetSerializer<TK>(out var s) ||
                ProvidedSerializers.TryGetSerializer(out s))
            {
                _keySerializer = s;
            }
        }

        public async Task<TV> Get(TK key)
        {
            var fromCache = await _cache.Get(new Key<TK>(key, _keySerializer));

            return fromCache.Value;
        }

        public async Task Set(TK key, TV value, TimeSpan timeToLive)
        {
            await _cache.Set(BuildKey(key), value, timeToLive);
        }

        public async Task<IDictionary<TK, TV>> Get(ICollection<TK> keys)
        {
            var fromCache = await _cache.Get(keys.Select(BuildKey).ToArray());

            return fromCache.ToDictionary(r => r.Key.AsObject, r => r.Value);
        }

        public async Task Set(ICollection<KeyValuePair<TK, TV>> values, TimeSpan timeToLive)
        {
            var forCache = values
                .Select(kv => new KeyValuePair<Key<TK>, TV>(BuildKey(kv.Key), kv.Value))
                .ToArray();
            
            await _cache.Set(forCache, timeToLive);
        }

        private Key<TK> BuildKey(TK key)
        {
            return new Key<TK>(key, _keySerializer);
        }
    }
}