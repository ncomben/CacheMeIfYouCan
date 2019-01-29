﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CacheMeIfYouCan.Internal;
using CacheMeIfYouCan.Serializers;

namespace CacheMeIfYouCan.Configuration
{
    public sealed class MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter, TKInnerEnumerable, TRes, TKInner, TV>
        : MultiParamEnumerableKeyFunctionCacheConfigurationManagerBase<MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter, TKInnerEnumerable, TRes, TKInner, TV>, TKOuter, TKInner, TV>
        where TKInnerEnumerable : IEnumerable<TKInner>
        where TRes : IDictionary<TKInner, TV>
    {
        internal MultiParamEnumerableKeyFunctionCacheConfigurationManager(
            Func<TKOuter, TKInnerEnumerable, Task<TRes>> inputFunc,
            string functionName)
            : base(
                inputFunc
                    .ConvertInputToEnumerable<TKOuter, TKInnerEnumerable, TRes, TKInner, TV>()
                    .ConvertOutputToDictionary<TKOuter, IEnumerable<TKInner>, TRes, TKInner, TV>(),
                functionName)
        { }

        internal MultiParamEnumerableKeyFunctionCacheConfigurationManager(
            Func<TKOuter, TKInnerEnumerable, Task<TRes>> inputFunc,
            CachedProxyConfig interfaceConfig,
            MethodInfo methodInfo)
            : base(
                inputFunc
                    .ConvertInputToEnumerable<TKOuter, TKInnerEnumerable, TRes, TKInner, TV>()
                    .ConvertOutputToDictionary<TKOuter, IEnumerable<TKInner>, TRes, TKInner, TV>(),
                interfaceConfig,
                methodInfo)
        { }
        
        public new MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer serializer)
        {
            WithKeySerializerInternal(serializer.Serialize, serializer.Deserialize<TKOuter>);
            WithKeySerializerInternal(serializer.Serialize, serializer.Deserialize<TKInner>);
            return this;
        }
        
        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer<TKOuter> serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize);
        }
        
        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer<TKInner> serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize);
        }
        
        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            Func<TKOuter, string> serializer, Func<string, TKOuter> deserializer = null)
        {
            return WithKeySerializerInternal(serializer, deserializer);
        }
        
        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            Func<TKInner, string> serializer, Func<string, TKInner> deserializer = null)
        {
            return WithKeySerializerInternal(serializer, deserializer);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter, TKInnerEnumerable, TRes, TKInner, TV> WithKeyComparer(
            IEqualityComparer<TKOuter> comparer)
        {
            return WithKeyComparerInternal(comparer);
        }
        
        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter, TKInnerEnumerable, TRes, TKInner, TV> WithKeyComparer(
            IEqualityComparer<TKInner> comparer)
        {
            return WithKeyComparerInternal(comparer);
        }
        
        public Func<TKOuter, TKInnerEnumerable, Task<TRes>> Build()
        {
            var functionCache = BuildMultiParamEnumerableKeyFunctionCache();

            var dictionaryFactoryFunc = DictionaryFactoryFunc ?? DictionaryFactoryFuncResolver.Get<TRes, TKInner, TV>();

            var keyComparer = KeyComparerResolver.GetInner<TKInner>();
            
            Func<TKOuter, IEnumerable<TKInner>, Task<IDictionary<TKInner, TV>>> func = GetResults;
            
            return func
                .ConvertInputFromEnumerable<TKOuter, TKInnerEnumerable, IDictionary<TKInner, TV>, TKInner, TV>()
                .ConvertOutputFromDictionary<TKOuter, TKInnerEnumerable, TRes, TKInner, TV>();

            async Task<IDictionary<TKInner, TV>> GetResults(TKOuter outerKey, IEnumerable<TKInner> innerKeys)
            {
                var results = await functionCache.GetMulti(outerKey, innerKeys);

                var dictionary = dictionaryFactoryFunc(keyComparer, results.Count);
                
                foreach (var kv in results)
                    dictionary[kv.Key.Item2] = kv.Value;

                return dictionary;
            }
        }
    }

    public sealed class MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV>
        : MultiParamEnumerableKeyFunctionCacheConfigurationManagerBase<MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV>, (TKOuter1, TKOuter2), TKInner, TV>
        where TKInnerEnumerable : IEnumerable<TKInner>
        where TRes : IDictionary<TKInner, TV>
    {
        internal MultiParamEnumerableKeyFunctionCacheConfigurationManager(
            Func<TKOuter1, TKOuter2, TKInnerEnumerable, Task<TRes>> inputFunc,
            string functionName)
            : base(
                inputFunc
                    .ConvertFunc()
                    .ConvertInputToEnumerable<(TKOuter1, TKOuter2), TKInnerEnumerable, TRes, TKInner, TV>()
                    .ConvertOutputToDictionary<(TKOuter1, TKOuter2), IEnumerable<TKInner>, TRes, TKInner, TV>(),
                functionName)
        {
        }

        internal MultiParamEnumerableKeyFunctionCacheConfigurationManager(
            Func<TKOuter1, TKOuter2, TKInnerEnumerable, Task<TRes>> inputFunc,
            CachedProxyConfig interfaceConfig,
            MethodInfo methodInfo)
            : base(
                inputFunc
                    .ConvertFunc()
                    .ConvertInputToEnumerable<(TKOuter1, TKOuter2), TKInnerEnumerable, TRes, TKInner, TV>()
                    .ConvertOutputToDictionary<(TKOuter1, TKOuter2), IEnumerable<TKInner>, TRes, TKInner, TV>(),
                interfaceConfig,
                methodInfo)
        {
        }
        
        public new MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer serializer)
        {
            WithKeySerializerInternal(serializer.Serialize, serializer.Deserialize<TKOuter1>);
            WithKeySerializerInternal(serializer.Serialize, serializer.Deserialize<TKOuter2>);
            WithKeySerializerInternal(serializer.Serialize, serializer.Deserialize<TKInner>);
            return this;
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer<TKOuter1> serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize);
        }
        
        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer<TKOuter2> serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer<TKInner> serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            Func<TKOuter1, string> serializer, Func<string, TKOuter1> deserializer = null)
        {
            return WithKeySerializerInternal(serializer, deserializer);
        }
        
        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            Func<TKOuter2, string> serializer, Func<string, TKOuter2> deserializer = null)
        {
            return WithKeySerializerInternal(serializer, deserializer);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            Func<TKInner, string> serializer, Func<string, TKInner> deserializer = null)
        {
            return WithKeySerializerInternal(serializer, deserializer);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV> WithKeyComparer(
            IEqualityComparer<TKOuter1> comparer)
        {
            return WithKeyComparerInternal(comparer);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV> WithKeyComparer(
            IEqualityComparer<TKOuter2> comparer)
        {
            return WithKeyComparerInternal(comparer);
        }
        
        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKInnerEnumerable, TRes, TKInner, TV> WithKeyComparer(
            IEqualityComparer<TKInner> comparer)
        {
            return WithKeyComparerInternal(comparer);
        }

        public Func<TKOuter1, TKOuter2, TKInnerEnumerable, Task<TRes>> Build()
        {
            var functionCache = BuildMultiParamEnumerableKeyFunctionCache(
                TupleKeyHelper.BuildKeyComparer<TKOuter1, TKOuter2>(KeyComparers),
                TupleKeyHelper.BuildKeySerializer<TKOuter1, TKOuter2>(KeySerializers, KeyParamSeparator));

            var dictionaryFactoryFunc = DictionaryFactoryFunc ?? DictionaryFactoryFuncResolver.Get<TRes, TKInner, TV>();

            var keyComparer = KeyComparerResolver.GetInner<TKInner>();

            Func<(TKOuter1, TKOuter2), IEnumerable<TKInner>, Task<IDictionary<TKInner, TV>>> func = GetResults;

            return func
                .ConvertInputFromEnumerable<(TKOuter1, TKOuter2), TKInnerEnumerable, IDictionary<TKInner, TV>, TKInner, TV>()
                .ConvertOutputFromDictionary<(TKOuter1, TKOuter2), TKInnerEnumerable, TRes, TKInner, TV>()
                .ConvertFunc();

            async Task<IDictionary<TKInner, TV>> GetResults(
                (TKOuter1, TKOuter2) outerKeys,
                IEnumerable<TKInner> innerKeys)
            {
                var results = await functionCache.GetMulti(outerKeys, innerKeys);

                var dictionary = dictionaryFactoryFunc(keyComparer, results.Count);

                foreach (var kv in results)
                    dictionary[kv.Key.Item2] = kv.Value;

                return dictionary;
            }
        }
    }

    public sealed class MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV>
        : MultiParamEnumerableKeyFunctionCacheConfigurationManagerBase<MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV>, (TKOuter1, TKOuter2, TKOuter3), TKInner, TV>
        where TKInnerEnumerable : IEnumerable<TKInner>
        where TRes : IDictionary<TKInner, TV>
    {
        internal MultiParamEnumerableKeyFunctionCacheConfigurationManager(
            Func<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, Task<TRes>> inputFunc,
            string functionName)
            : base(
                inputFunc
                    .ConvertFunc()
                    .ConvertInputToEnumerable<(TKOuter1, TKOuter2, TKOuter3), TKInnerEnumerable, TRes, TKInner, TV>()
                    .ConvertOutputToDictionary<(TKOuter1, TKOuter2, TKOuter3), IEnumerable<TKInner>, TRes, TKInner, TV>(),
                functionName)
        {
        }

        internal MultiParamEnumerableKeyFunctionCacheConfigurationManager(
            Func<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, Task<TRes>> inputFunc,
            CachedProxyConfig interfaceConfig,
            MethodInfo methodInfo)
            : base(
                inputFunc
                    .ConvertFunc()
                    .ConvertInputToEnumerable<(TKOuter1, TKOuter2, TKOuter3), TKInnerEnumerable, TRes, TKInner, TV>()
                    .ConvertOutputToDictionary<(TKOuter1, TKOuter2, TKOuter3), IEnumerable<TKInner>, TRes, TKInner, TV>(),
                interfaceConfig,
                methodInfo)
        {
        }

        public new MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer serializer)
        {
            WithKeySerializerInternal(serializer.Serialize, serializer.Deserialize<TKOuter1>);
            WithKeySerializerInternal(serializer.Serialize, serializer.Deserialize<TKOuter2>);
            WithKeySerializerInternal(serializer.Serialize, serializer.Deserialize<TKOuter3>);
            WithKeySerializerInternal(serializer.Serialize, serializer.Deserialize<TKInner>);
            return this;
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer<TKOuter1> serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer<TKOuter2> serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer<TKOuter3> serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            ISerializer<TKInner> serializer)
        {
            return WithKeySerializer(serializer.Serialize, serializer.Deserialize);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            Func<TKOuter1, string> serializer, Func<string, TKOuter1> deserializer = null)
        {
            return WithKeySerializerInternal(serializer, deserializer);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            Func<TKOuter2, string> serializer, Func<string, TKOuter2> deserializer = null)
        {
            return WithKeySerializerInternal(serializer, deserializer);
        }
        
        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            Func<TKOuter3, string> serializer, Func<string, TKOuter3> deserializer = null)
        {
            return WithKeySerializerInternal(serializer, deserializer);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeySerializer(
            Func<TKInner, string> serializer, Func<string, TKInner> deserializer = null)
        {
            return WithKeySerializerInternal(serializer, deserializer);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeyComparer(
            IEqualityComparer<TKOuter1> comparer)
        {
            return WithKeyComparerInternal(comparer);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeyComparer(
            IEqualityComparer<TKOuter2> comparer)
        {
            return WithKeyComparerInternal(comparer);
        }

        public MultiParamEnumerableKeyFunctionCacheConfigurationManager<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, TRes, TKInner, TV> WithKeyComparer(
            IEqualityComparer<TKInner> comparer)
        {
            return WithKeyComparerInternal(comparer);
        }

        public Func<TKOuter1, TKOuter2, TKOuter3, TKInnerEnumerable, Task<TRes>> Build()
        {
            var functionCache = BuildMultiParamEnumerableKeyFunctionCache(
                TupleKeyHelper.BuildKeyComparer<TKOuter1, TKOuter2, TKOuter3>(KeyComparers),
                TupleKeyHelper.BuildKeySerializer<TKOuter1, TKOuter2, TKOuter3>(KeySerializers, KeyParamSeparator));
            
            var dictionaryFactoryFunc = DictionaryFactoryFunc ?? DictionaryFactoryFuncResolver.Get<TRes, TKInner, TV>();

            var keyComparer = KeyComparerResolver.GetInner<TKInner>();

            Func<(TKOuter1, TKOuter2, TKOuter3), IEnumerable<TKInner>, Task<IDictionary<TKInner, TV>>>
                func = GetResults;

            return func
                .ConvertInputFromEnumerable<(TKOuter1, TKOuter2, TKOuter3), TKInnerEnumerable, IDictionary<TKInner, TV>, TKInner, TV>()
                .ConvertOutputFromDictionary<(TKOuter1, TKOuter2, TKOuter3), TKInnerEnumerable, TRes, TKInner, TV>()
                .ConvertFunc();

            async Task<IDictionary<TKInner, TV>> GetResults(
                (TKOuter1, TKOuter2, TKOuter3) outerKeys,
                IEnumerable<TKInner> innerKeys)
            {
                var results = await functionCache.GetMulti(outerKeys, innerKeys);

                var dictionary = dictionaryFactoryFunc(keyComparer, results.Count);

                foreach (var kv in results)
                    dictionary[kv.Key.Item2] = kv.Value;

                return dictionary;
            }
        }
    }
}