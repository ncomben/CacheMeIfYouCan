﻿﻿using System;
using System.Threading;
using System.Threading.Tasks;
using CacheMeIfYouCan.Internal;

namespace CacheMeIfYouCan.Configuration.SingleKey
{
    public abstract class CachedFunctionConfigurationManagerAsyncBase<TParams, TKey, TValue, TConfig>
        : CachedFunctionConfigurationManagerBase<TParams, TKey, TValue, TConfig>
        where TConfig : class, ISingleKeyCachedFunctionConfigurationManagerBase<TParams, TKey, TValue, TConfig>
    {
        private readonly Func<TParams, Task<TValue>> _originalFunction;
        private readonly Func<TParams, TKey> _cacheKeySelector;
        
        internal CachedFunctionConfigurationManagerAsyncBase(
            Func<TParams, Task<TValue>> originalFunction,
            Func<TParams, TKey> cacheKeySelector)
        {
            _originalFunction = originalFunction;
            _cacheKeySelector = cacheKeySelector;
        }

        private protected Func<TParams, Task<TValue>> BuildInternal()
        {
            var cachedFunction = BuildCachedFunction(ConvertFunction(), _cacheKeySelector);

            return Get;
            
            Task<TValue> Get(TParams parameters)
            {
                return cachedFunction.Get(parameters, CancellationToken.None).AsTask();
            }
        }

        private Func<TParams, CancellationToken, ValueTask<TValue>> ConvertFunction()
        {
            return (keys, _) => new ValueTask<TValue>(_originalFunction(keys));
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerAsync_1Param<TParam, TValue> :
        CachedFunctionConfigurationManagerAsyncBase<TParam, TParam, TValue, ISingleKeyCachedFunctionConfigurationManagerAsync_1Param<TParam, TParam, TValue>>,
        ISingleKeyCachedFunctionConfigurationManagerAsync_1Param_KeySelector<TParam, TValue>
    {
        private readonly Func<TParam, Task<TValue>> _originalFunction;

        internal CachedFunctionConfigurationManagerAsync_1Param(
            Func<TParam, Task<TValue>> originalFunction)
            : base(originalFunction, p => p)
        {
            _originalFunction = originalFunction;
        }

        public ISingleKeyCachedFunctionConfigurationManagerAsync_1Param<TParam, TKey, TValue> WithCacheKey<TKey>(Func<TParam, TKey> cacheKeySelector)
        {
            return new CachedFunctionConfigurationManagerAsync_1Param<TParam, TKey, TValue>(_originalFunction, cacheKeySelector);
        }

        public Func<TParam, Task<TValue>> Build() => BuildInternal();
    }
    
    public sealed class CachedFunctionConfigurationManagerAsync_1Param<TParam, TKey, TValue>
        : CachedFunctionConfigurationManagerAsyncBase<TParam, TKey, TValue, ISingleKeyCachedFunctionConfigurationManagerAsync_1Param<TParam, TKey, TValue>>,
            ISingleKeyCachedFunctionConfigurationManagerAsync_1Param<TParam, TKey, TValue>
    {
        internal CachedFunctionConfigurationManagerAsync_1Param(
            Func<TParam, Task<TValue>> originalFunction,
            Func<TParam, TKey> cacheKeySelector)
            : base(originalFunction, cacheKeySelector)
        { }

        public Func<TParam, Task<TValue>> Build() => BuildInternal();
    }
    
    public sealed class CachedFunctionConfigurationManagerAsync_2Params<TParam1, TParam2, TKey, TValue>
        : CachedFunctionConfigurationManagerAsyncBase<(TParam1, TParam2), TKey, TValue, CachedFunctionConfigurationManagerAsync_2Params<TParam1, TParam2, TKey, TValue>>
    {
        internal CachedFunctionConfigurationManagerAsync_2Params(
            Func<TParam1, TParam2, Task<TValue>> originalFunction,
            Func<TParam1, TParam2, TKey> cacheKeySelector)
            : base(
                TupleHelper.ConvertFuncToTupleInput(originalFunction),
                TupleHelper.ConvertFuncToTupleInput(cacheKeySelector))
        { }

        public Func<TParam1, TParam2, Task<TValue>> Build()
        {
            var cachedFunction = BuildInternal();

            return TupleHelper.ConvertFuncFromTupleInput(cachedFunction);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerAsync_3Params<TParam1, TParam2, TParam3, TKey, TValue>
        : CachedFunctionConfigurationManagerAsyncBase<(TParam1, TParam2, TParam3), TKey, TValue, CachedFunctionConfigurationManagerAsync_3Params<TParam1, TParam2, TParam3, TKey, TValue>>
    {
        internal CachedFunctionConfigurationManagerAsync_3Params(
            Func<TParam1, TParam2, TParam3, Task<TValue>> originalFunction,
            Func<TParam1, TParam2, TParam3, TKey> cacheKeySelector)
            : base(
                TupleHelper.ConvertFuncToTupleInput(originalFunction),
                TupleHelper.ConvertFuncToTupleInput(cacheKeySelector))
        { }

        public Func<TParam1, TParam2, TParam3, Task<TValue>> Build()
        {
            var cachedFunction = BuildInternal();

            return TupleHelper.ConvertFuncFromTupleInput(cachedFunction);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerAsync_4Params<TParam1, TParam2, TParam3, TParam4, TKey, TValue>
        : CachedFunctionConfigurationManagerAsyncBase<(TParam1, TParam2, TParam3, TParam4), TKey, TValue, CachedFunctionConfigurationManagerAsync_4Params<TParam1, TParam2, TParam3, TParam4, TKey, TValue>>
    {
        internal CachedFunctionConfigurationManagerAsync_4Params(
            Func<TParam1, TParam2, TParam3, TParam4, Task<TValue>> originalFunction,
            Func<TParam1, TParam2, TParam3, TParam4, TKey> cacheKeySelector)
            : base(
                TupleHelper.ConvertFuncToTupleInput(originalFunction),
                TupleHelper.ConvertFuncToTupleInput(cacheKeySelector))
        { }

        public Func<TParam1, TParam2, TParam3, TParam4, Task<TValue>> Build()
        {
            var cachedFunction = BuildInternal();

            return TupleHelper.ConvertFuncFromTupleInput(cachedFunction);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerAsync_5Params<TParam1, TParam2, TParam3, TParam4, TParam5, TKey, TValue>
        : CachedFunctionConfigurationManagerAsyncBase<(TParam1, TParam2, TParam3, TParam4, TParam5), TKey, TValue, CachedFunctionConfigurationManagerAsync_5Params<TParam1, TParam2, TParam3, TParam4, TParam5, TKey, TValue>>
    {
        internal CachedFunctionConfigurationManagerAsync_5Params(
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, Task<TValue>> originalFunction,
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, TKey> cacheKeySelector)
            : base(
                TupleHelper.ConvertFuncToTupleInput(originalFunction),
                TupleHelper.ConvertFuncToTupleInput(cacheKeySelector))
        { }

        public Func<TParam1, TParam2, TParam3, TParam4, TParam5, Task<TValue>> Build()
        {
            var cachedFunction = BuildInternal();

            return TupleHelper.ConvertFuncFromTupleInput(cachedFunction);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerAsync_6Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKey, TValue>
        : CachedFunctionConfigurationManagerAsyncBase<(TParam1, TParam2, TParam3, TParam4, TParam5, TParam6), TKey, TValue, CachedFunctionConfigurationManagerAsync_6Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKey, TValue>>
    {
        internal CachedFunctionConfigurationManagerAsync_6Params(
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, Task<TValue>> originalFunction,
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TKey> cacheKeySelector)
            : base(
                TupleHelper.ConvertFuncToTupleInput(originalFunction),
                TupleHelper.ConvertFuncToTupleInput(cacheKeySelector))
        { }

        public Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, Task<TValue>> Build()
        {
            var cachedFunction = BuildInternal();

            return TupleHelper.ConvertFuncFromTupleInput(cachedFunction);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerAsync_7Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKey, TValue>
        : CachedFunctionConfigurationManagerAsyncBase<(TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7), TKey, TValue, CachedFunctionConfigurationManagerAsync_7Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKey, TValue>>
    {
        internal CachedFunctionConfigurationManagerAsync_7Params(
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, Task<TValue>> originalFunction,
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TKey> cacheKeySelector)
            : base(
                TupleHelper.ConvertFuncToTupleInput(originalFunction),
                TupleHelper.ConvertFuncToTupleInput(cacheKeySelector))
        { }

        public Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, Task<TValue>> Build()
        {
            var cachedFunction = BuildInternal();

            return TupleHelper.ConvertFuncFromTupleInput(cachedFunction);
        }
    }
    
    public sealed class CachedFunctionConfigurationManagerAsync_8Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TKey, TValue>
        : CachedFunctionConfigurationManagerAsyncBase<(TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8), TKey, TValue, CachedFunctionConfigurationManagerAsync_8Params<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TKey, TValue>>
    {
        internal CachedFunctionConfigurationManagerAsync_8Params(
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, Task<TValue>> originalFunction,
            Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TKey> cacheKeySelector)
            : base(
                TupleHelper.ConvertFuncToTupleInput(originalFunction),
                TupleHelper.ConvertFuncToTupleInput(cacheKeySelector))
        { }

        public Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, Task<TValue>> Build()
        {
            var cachedFunction = BuildInternal();

            return TupleHelper.ConvertFuncFromTupleInput(cachedFunction);
        }
    }
}