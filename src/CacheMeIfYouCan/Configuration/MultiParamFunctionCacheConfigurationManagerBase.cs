using System;
using System.Reflection;
using System.Threading.Tasks;
using CacheMeIfYouCan.Internal;

namespace CacheMeIfYouCan.Configuration
{
    public abstract class MultiParamFunctionCacheConfigurationManagerBase<TConfig, TK, TV>
        : SingleKeyFunctionCacheConfigurationManagerBase<TConfig, TK, TV>
        where TConfig : MultiParamFunctionCacheConfigurationManagerBase<TConfig, TK, TV>
    {
        protected string KeyParamSeparator { get; private set; }

        internal MultiParamFunctionCacheConfigurationManagerBase(
            Func<TK, Task<TV>> inputFunc,
            string functionName)
            : base(inputFunc, functionName)
        {
            KeyParamSeparator = DefaultSettings.Cache.KeyParamSeparator;
        }

        internal MultiParamFunctionCacheConfigurationManagerBase(
            Func<TK, Task<TV>> inputFunc,
            CachedProxyConfig interfaceConfig,
            MethodInfo methodInfo)
            : base(
                inputFunc,
                interfaceConfig,
                methodInfo)
        {
            KeyParamSeparator = interfaceConfig.KeyParamSeparator;
        }

        public TConfig WithKeyParamSeparator(string separator)
        {
            KeyParamSeparator = separator;
            return (TConfig)this;
        }
    }
}