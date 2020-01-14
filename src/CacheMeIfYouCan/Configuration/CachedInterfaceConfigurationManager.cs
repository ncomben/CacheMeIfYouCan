﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CacheMeIfYouCan.Configuration.EnumerableKeys;
using CacheMeIfYouCan.Configuration.SingleKey;
using CacheMeIfYouCan.Internal;

namespace CacheMeIfYouCan.Configuration
{
    public sealed class CachedInterfaceConfigurationManager<T>
    {
        private readonly T _originalImpl;
        private readonly HashSet<MethodInfo> _allInterfaceMethods; 
        private readonly Dictionary<MethodInfo, object> _functionCacheConfigActions;

        internal CachedInterfaceConfigurationManager(T originalImpl)
        {
            _originalImpl = originalImpl;
            _allInterfaceMethods = new HashSet<MethodInfo>(InterfaceMethodsResolver.GetAllMethods(typeof(T)));
            _functionCacheConfigActions = new Dictionary<MethodInfo, object>();
        }

        public CachedInterfaceConfigurationManager<T> Configure<TKey, TValue>(
            Expression<Func<T, Func<TKey, Task<TValue>>>> expression,
            Action<CachedFunctionConfigurationManagerAsync<TKey, TValue>> configurationAction)
        {
            AddConfigAction(expression, configurationAction);
            return this;
        }

        public CachedInterfaceConfigurationManager<T> Configure<TKey, TValue>(
            Expression<Func<T, Func<TKey, TValue>>> expression,
            Action<CachedFunctionConfigurationManagerSync<TKey, TValue>> configurationAction)
        {
            AddConfigAction(expression, configurationAction);
            return this;
        }

        public CachedInterfaceConfigurationManager<T> Configure<TKey, TValue>(
            Expression<Func<T, Func<TKey, CancellationToken, Task<TValue>>>> expression,
            Action<CachedFunctionConfigurationManagerAsyncCanx<TKey, TValue>> configurationAction)
        {
            AddConfigAction(expression, configurationAction);
            return this;
        }
        
        public CachedInterfaceConfigurationManager<T> Configure<TKey, TValue>(
            Expression<Func<T, Func<TKey, CancellationToken, TValue>>> expression,
            Action<CachedFunctionConfigurationManagerSyncCanx<TKey, TValue>> configurationAction)
        {
            AddConfigAction(expression, configurationAction);
            return this;
        }
        
        public CachedInterfaceConfigurationManager<T> Configure<TKey, TValue, TRequest, TResponse>(
            Expression<Func<T, Func<TRequest, Task<TResponse>>>> expression,
            Action<CachedFunctionConfigurationManagerAsync<TKey, TValue, TRequest, TResponse>> configurationAction)
            where TRequest : IEnumerable<TKey>
            where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            AddConfigAction(expression, configurationAction);
            return this;
        }
        
        public CachedInterfaceConfigurationManager<T> Configure<TKey, TValue, TRequest, TResponse>(
            Expression<Func<T, Func<TRequest, TResponse>>> expression,
            Action<CachedFunctionConfigurationManagerSync<TKey, TValue, TRequest, TResponse>> configurationAction)
            where TRequest : IEnumerable<TKey>
            where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            AddConfigAction(expression, configurationAction);
            return this;
        }

        public CachedInterfaceConfigurationManager<T> Configure<TKey, TValue, TRequest, TResponse>(
            Expression<Func<T, Func<TRequest, CancellationToken, Task<TResponse>>>> expression,
            Action<CachedFunctionConfigurationManagerAsyncCanx<TKey, TValue, TRequest, TResponse>> configurationAction)
            where TRequest : IEnumerable<TKey>
            where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            AddConfigAction(expression, configurationAction);
            return this;
        }
        
        public CachedInterfaceConfigurationManager<T> Configure<TKey, TValue, TRequest, TResponse>(
            Expression<Func<T, Func<TRequest, CancellationToken, TResponse>>> expression,
            Action<CachedFunctionConfigurationManagerSyncCanx<TKey, TValue, TRequest, TResponse>> configurationAction)
            where TRequest : IEnumerable<TKey>
            where TResponse : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            AddConfigAction(expression, configurationAction);
            return this;
        }

        public T Build() => CachedInterfaceFactoryInternal.Build(_originalImpl, _functionCacheConfigActions);

        private void AddConfigAction<TFunc>(Expression<Func<T, TFunc>> expression, object configurationAction)
        {
            var methodInfo = GetMethodInfo(expression);
            
            if (_functionCacheConfigActions.ContainsKey(methodInfo))
                throw new Exception($"Duplicate configuration for {methodInfo}");
            
            _functionCacheConfigActions.Add(methodInfo, configurationAction);
        }
        
        private MethodInfo GetMethodInfo(LambdaExpression expression)
        {
            var unaryExpression = (UnaryExpression)expression.Body;
            var methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
            var methodCallObject = (ConstantExpression)methodCallExpression.Object;
            var methodInfo = (MethodInfo)methodCallObject.Value;
            
            if (!_allInterfaceMethods.Contains(methodInfo))
                throw new InvalidOperationException($"Expression:'{expression.Body} does not match any methods on {typeof(T).Name}");

            return methodInfo;
        }
    }
}