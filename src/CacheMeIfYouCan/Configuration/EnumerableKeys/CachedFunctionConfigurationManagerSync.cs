﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CacheMeIfYouCan.Configuration.OuterKeyAndInnerEnumerableKeys;

namespace CacheMeIfYouCan.Configuration.EnumerableKeys
{
    public abstract class CachedFunctionConfigurationManagerSyncBase<TParams, TKeys, TResponse, TKey, TValue, TConfig>
        : CachedFunctionConfigurationManagerBase<TParams, TKeys, TResponse, TKey, TValue, TConfig>
        where TKeys : IEnumerable<TKey>
        where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
        where TConfig : CachedFunctionConfigurationManagerBase<TParams, TKeys, TResponse, TKey, TValue, TConfig>
    {
        private readonly Func<TParams, TKeys, TResponse> _originalFunction;

        internal CachedFunctionConfigurationManagerSyncBase(Func<TParams, TKeys, TResponse> originalFunction)
        {
            _originalFunction = originalFunction;
        }

        private protected Func<TParams, TKeys, TResponse> BuildInternal()
        {
            var cachedFunction = BuildCachedFunction(ConvertFunction());

            var responseConverter = GetResponseConverter();
            
            return Get;
            
            TResponse Get(TParams parameters, TKeys request)
            {
                var task = cachedFunction.GetMany(parameters, request, CancellationToken.None);

                var results = task.IsCompleted
                    ? task.Result
                    : task.GetAwaiter().GetResult();

                return results switch
                {
                    null => default,
                    TResponse typedResponse => typedResponse,
                    _ => responseConverter(task.Result)
                };
            }
        }

        private Func<TParams, IReadOnlyCollection<TKey>, CancellationToken, ValueTask<IEnumerable<KeyValuePair<TKey, TValue>>>> ConvertFunction()
        {
            var requestConverter = GetRequestConverter();

            return Get;
            
            ValueTask<IEnumerable<KeyValuePair<TKey, TValue>>> Get(
                TParams parameters,
                IReadOnlyCollection<TKey> keys,
                CancellationToken cancellationToken)
            {
                if (!(keys is TKeys typedRequest))
                    typedRequest = requestConverter(keys);

                return new ValueTask<IEnumerable<KeyValuePair<TKey, TValue>>>(_originalFunction(parameters, typedRequest));
            }
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerSync<TKeys, TResponse, TKey, TValue>
        : CachedFunctionConfigurationManagerSyncBase<Unit, TKeys, TResponse, TKey, TValue, CachedFunctionConfigurationManagerSync<TKeys, TResponse, TKey, TValue>>,
            ICachedFunctionConfigurationManagerSync_1Param<TKeys, TResponse>
        where TKeys : IEnumerable<TKey>
        where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        internal CachedFunctionConfigurationManagerSync(Func<TKeys, TResponse> originalFunction)
            : base((_, keys) => originalFunction(keys))
        { }

        public Func<TKeys, TResponse> Build()
        {
            var cachedFunction = BuildInternal();

            return keys => cachedFunction(null, keys);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerSync_2Params<TParam, TKeys, TResponse, TKey, TValue>
        : CachedFunctionConfigurationManagerSyncBase<TParam, TKeys, TResponse, TKey, TValue, CachedFunctionConfigurationManagerSync_2Params<TParam, TKeys, TResponse, TKey, TValue>>,
            ICachedFunctionConfigurationManagerSync_2Params<TParam, TKeys, TResponse>
        where TKeys : IEnumerable<TKey>
        where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Func<TParam, TKeys, TResponse> _originalFunction;

        internal CachedFunctionConfigurationManagerSync_2Params(Func<TParam, TKeys, TResponse> originalFunction)
            : base(originalFunction)
        {
            _originalFunction = originalFunction;
        }

        public CachedFunctionConfigurationManagerSync<TParam, TKeys, TResponse, TParam, TKey, TValue> UseFirstParamAsOuterCacheKey()
        {
            return new CachedFunctionConfigurationManagerSync<TParam, TKeys, TResponse, TParam, TKey, TValue>(_originalFunction, p => p);
        }
        
        public CachedFunctionConfigurationManagerSync<TParam, TKeys, TResponse, TOuterKey, TKey, TValue> WithOuterCacheKey<TOuterKey>(
            Func<TParam, TOuterKey> keySelector)
        {
            return new CachedFunctionConfigurationManagerSync<TParam, TKeys, TResponse, TOuterKey, TKey, TValue>(_originalFunction, keySelector);
        }

        public Func<TParam, TKeys, TResponse> Build() => BuildInternal();
    }
    
    public sealed class CachedFunctionConfigurationManagerSync_3Params<TParam1, TParam2, TKeys, TResponse, TKey, TValue>
        : CachedFunctionConfigurationManagerSyncBase<(TParam1, TParam2), TKeys, TResponse, TKey, TValue, CachedFunctionConfigurationManagerSync_3Params<TParam1, TParam2, TKeys, TResponse, TKey, TValue>>,
            ICachedFunctionConfigurationManagerSync_3Params<TParam1, TParam2, TKeys, TResponse>
        where TKeys : IEnumerable<TKey>
        where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Func<TParam1, TParam2, TKeys, TResponse> _originalFunction;

        internal CachedFunctionConfigurationManagerSync_3Params(Func<TParam1, TParam2, TKeys, TResponse> originalFunction)
            : base((t, keys) => originalFunction(t.Item1, t.Item2, keys))
        {
            _originalFunction = originalFunction;
        }
        
        public CachedFunctionConfigurationManagerSync_3Params<TParam1, TParam2, TKeys, TResponse, TOuterKey, TKey, TValue> WithOuterCacheKey<TOuterKey>(
            Func<TParam1, TParam2, TOuterKey> keySelector)
        {
            return new CachedFunctionConfigurationManagerSync_3Params<TParam1, TParam2, TKeys, TResponse, TOuterKey, TKey, TValue>(_originalFunction, keySelector);
        }

        public Func<TParam1, TParam2, TKeys, TResponse> Build()
        {
            var cachedFunction = BuildInternal();

            return (param1, param2, keys) => cachedFunction((param1, param2), keys);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerSync_4Params<TParam1, TParam2, TParam3, TKeys, TResponse, TKey, TValue>
        : CachedFunctionConfigurationManagerSyncBase<(TParam1, TParam2, TParam3), TKeys, TResponse, TKey, TValue, CachedFunctionConfigurationManagerSync_4Params<TParam1, TParam2, TParam3, TKeys, TResponse, TKey, TValue>>,
            ICachedFunctionConfigurationManagerSync_4Params<TParam1, TParam2, TParam3, TKeys, TResponse>
        where TKeys : IEnumerable<TKey>
        where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Func<TParam1, TParam2, TParam3, TKeys, TResponse> _originalFunction;

        internal CachedFunctionConfigurationManagerSync_4Params(Func<TParam1, TParam2, TParam3, TKeys, TResponse> originalFunction)
            : base((t, keys) => originalFunction(t.Item1, t.Item2, t.Item3, keys))
        {
            _originalFunction = originalFunction;
        }
        
        public CachedFunctionConfigurationManagerSync_4Params<TParam1, TParam2, TParam3, TKeys, TResponse, TOuterKey, TKey, TValue> WithOuterCacheKey<TOuterKey>(
            Func<TParam1, TParam2, TParam3, TOuterKey> keySelector)
        {
            return new CachedFunctionConfigurationManagerSync_4Params<TParam1, TParam2, TParam3, TKeys, TResponse, TOuterKey, TKey, TValue>(_originalFunction, keySelector);
        }

        public Func<TParam1, TParam2, TParam3, TKeys, TResponse> Build()
        {
            var cachedFunction = BuildInternal();

            return (param1, param2, param3, keys) => cachedFunction((param1, param2, param3), keys);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerSync_5Params<TParam1, TParam2, TParam3, TParam4, TKeys, TResponse, TKey, TValue>
        : CachedFunctionConfigurationManagerSyncBase<(TParam1, TParam2, TParam3, TParam4), TKeys, TResponse, TKey, TValue, CachedFunctionConfigurationManagerSync_5Params<TParam1, TParam2, TParam3, TParam4, TKeys, TResponse, TKey, TValue>>,
            ICachedFunctionConfigurationManagerSync_5Params<TParam1, TParam2, TParam3, TParam4, TKeys, TResponse>
        where TKeys : IEnumerable<TKey>
        where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Func<TParam1, TParam2, TParam3, TParam4, TKeys, TResponse> _originalFunction;

        internal CachedFunctionConfigurationManagerSync_5Params(Func<TParam1, TParam2, TParam3, TParam4, TKeys, TResponse> originalFunction)
            : base((t, keys) => originalFunction(t.Item1, t.Item2, t.Item3, t.Item4, keys))
        {
            _originalFunction = originalFunction;
        }
        
        public CachedFunctionConfigurationManagerSync_5Params<TParam1, TParam2, TParam3, TParam4, TKeys, TResponse, TOuterKey, TKey, TValue> WithOuterCacheKey<TOuterKey>(
            Func<TParam1, TParam2, TParam3, TParam4, TOuterKey> keySelector)
        {
            return new CachedFunctionConfigurationManagerSync_5Params<TParam1, TParam2, TParam3, TParam4, TKeys, TResponse, TOuterKey, TKey, TValue>(_originalFunction, keySelector);
        }

        public Func<TParam1, TParam2, TParam3, TParam4, TKeys, TResponse> Build()
        {
            var cachedFunction = BuildInternal();

            return (param1, param2, param3, param4, keys) => cachedFunction((param1, param2, param3, param4), keys);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerSync_6Params<TParam1, TParam2, TParam3, TParam4, TParam5, TKeys, TResponse, TKey, TValue>
        : CachedFunctionConfigurationManagerSyncBase<(TParam1, TParam2, TParam3, TParam4, TParam5), TKeys, TResponse, TKey, TValue, CachedFunctionConfigurationManagerSync_6Params<TParam1, TParam2, TParam3, TParam4, TParam5, TKeys, TResponse, TKey, TValue>>,
            ICachedFunctionConfigurationManagerSync_6Params<TParam1, TParam2, TParam3, TParam4, TParam5, TKeys, TResponse>
        where TKeys : IEnumerable<TKey>
        where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Func<TParam1, TParam2, TParam3, TParam4, TParam5, TKeys, TResponse> _originalFunction;

        internal CachedFunctionConfigurationManagerSync_6Params(Func<TParam1, TParam2, TParam3, TParam4, TParam5, TKeys, TResponse> originalFunction)
            : base((t, keys) => originalFunction(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, keys))
        {
            _originalFunction = originalFunction;
        }
        
        public CachedFunctionConfigurationManagerSync_6Params<TParam1, TParam2, TParam3, TParam4, TParam5, TKeys, TResponse, TOuterKey, TKey, TValue> WithOuterCacheKey<TOuterKey>(
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, TOuterKey> keySelector)
        {
            return new CachedFunctionConfigurationManagerSync_6Params<TParam1, TParam2, TParam3, TParam4, TParam5, TKeys, TResponse, TOuterKey, TKey, TValue>(_originalFunction, keySelector);
        }

        public Func<TParam1, TParam2, TParam3, TParam4, TParam5, TKeys, TResponse> Build()
        {
            var cachedFunction = BuildInternal();

            return (param1, param2, param3, param4, param5, keys) =>
                cachedFunction((param1, param2, param3, param4, param5), keys);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerSync_7Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKeys, TResponse, TKey, TValue>
        : CachedFunctionConfigurationManagerSyncBase<(TParam1, TParam2, TParam3, TParam4, TParam5, TParam6), TKeys, TResponse, TKey, TValue, CachedFunctionConfigurationManagerSync_7Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKeys, TResponse, TKey, TValue>>,
            ICachedFunctionConfigurationManagerSync_7Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKeys, TResponse>
        where TKeys : IEnumerable<TKey>
        where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKeys, TResponse> _originalFunction;

        internal CachedFunctionConfigurationManagerSync_7Params(Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKeys, TResponse> originalFunction)
            : base((t, keys) => originalFunction(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, keys))
        {
            _originalFunction = originalFunction;
        }
        
        public CachedFunctionConfigurationManagerSync_7Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKeys, TResponse, TOuterKey, TKey, TValue> WithOuterCacheKey<TOuterKey>(
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TOuterKey> keySelector)
        {
            return new CachedFunctionConfigurationManagerSync_7Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKeys, TResponse, TOuterKey, TKey, TValue>(_originalFunction, keySelector);
        }

        public Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKeys, TResponse> Build()
        {
            var cachedFunction = BuildInternal();

            return (param1, param2, param3, param4, param5, param6, keys) =>
                cachedFunction((param1, param2, param3, param4, param5, param6), keys);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerSync_8Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKeys, TResponse, TKey, TValue>
        : CachedFunctionConfigurationManagerSyncBase<(TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7), TKeys, TResponse, TKey, TValue, CachedFunctionConfigurationManagerSync_8Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKeys, TResponse, TKey, TValue>>,
            ICachedFunctionConfigurationManagerSync_8Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKeys, TResponse>
        where TKeys : IEnumerable<TKey>
        where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKeys, TResponse> _originalFunction;

        internal CachedFunctionConfigurationManagerSync_8Params(Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKeys, TResponse> originalFunction)
            : base((t, keys) => originalFunction(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, t.Item7, keys))
        {
            _originalFunction = originalFunction;
        }
        
        public CachedFunctionConfigurationManagerSync_8Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKeys, TResponse, TOuterKey, TKey, TValue> WithOuterCacheKey<TOuterKey>(
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TOuterKey> keySelector)
        {
            return new CachedFunctionConfigurationManagerSync_8Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKeys, TResponse, TOuterKey, TKey, TValue>(_originalFunction, keySelector);
        }

        public Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKeys, TResponse> Build()
        {
            var cachedFunction = BuildInternal();

            return (param1, param2, param3, param4, param5, param6, param7, keys) =>
                cachedFunction((param1, param2, param3, param4, param5, param6, param7), keys);
        }
    }
}