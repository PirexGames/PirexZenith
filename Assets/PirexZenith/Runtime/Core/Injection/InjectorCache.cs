using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;

namespace PirexZenith
{
    /// <summary>
    /// Manages and caches dependency injection delegates for each Type.
    /// Supports ahead-of-time registration from Source Generators or builds cached runtime reflection delegates.
    /// </summary>
    public static class InjectorCache
    {
        private static readonly Dictionary<Type, Action<IResolver, object>> _injectors = new Dictionary<Type, Action<IResolver, object>>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers a static injector delegate (used by Source Generators or generated bootstrap code).
        /// </summary>
        public static void RegisterInjector(Type type, Action<IResolver, object> injector)
        {
            lock (_lock)
            {
                _injectors[type] = injector;
            }
        }

        /// <summary>
        /// Performs dependency injection on the target by invoking or building a cached injector delegate.
        /// </summary>
        public static void Inject(IResolver resolver, object target)
        {
            if (target == null) return;

            Type type = target.GetType();
            Action<IResolver, object> injector = GetOrBuildInjector(type);
            injector?.Invoke(resolver, target);
        }

        /// <summary>
        /// Checks whether a specific Type requires member/property/method injection.
        /// </summary>
        public static bool RequiresInjection(Type type)
        {
            return GetOrBuildInjector(type) != null;
        }

        private static Action<IResolver, object> GetOrBuildInjector(Type type)
        {
            lock (_lock)
            {
                if (_injectors.TryGetValue(type, out var cached))
                {
                    return cached;
                }

                var injector = BuildExpressionInjector(type);
                _injectors[type] = injector;
                return injector;
            }
        }

        private static Action<IResolver, object> BuildExpressionInjector(Type type)
        {
            var expressions = new List<Expression>();
            var resolverParam = Expression.Parameter(typeof(IResolver), "resolver");
            var targetParam = Expression.Parameter(typeof(object), "target");
            var typedTarget = Expression.Variable(type, "typedTarget");

            expressions.Add(Expression.Assign(typedTarget, Expression.Convert(targetParam, type)));

            var resolveMethod = typeof(IResolver).GetMethod(nameof(IResolver.Resolve), new[] { typeof(Type) });
            int injectionCount = 0;

            Type currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                // 1. Fields (Zero-boxing compiled field assignment)
                foreach (var field in currentType.GetFields(flags))
                {
                    if (Attribute.IsDefined(field, typeof(InjectAttribute)))
                    {
                        var callResolve = Expression.Call(resolverParam, resolveMethod, Expression.Constant(field.FieldType));
                        var convertedVal = Expression.Convert(callResolve, field.FieldType);

                        if (field.IsInitOnly)
                        {
                            var fieldInfo = field;
                            expressions.Add(Expression.Call(
                                Expression.Constant(fieldInfo),
                                typeof(FieldInfo).GetMethod(nameof(FieldInfo.SetValue), new[] { typeof(object), typeof(object) }),
                                targetParam,
                                Expression.Convert(convertedVal, typeof(object))
                            ));
                        }
                        else
                        {
                            expressions.Add(Expression.Assign(Expression.Field(typedTarget, field), convertedVal));
                        }
                        injectionCount++;
                    }
                }

                // 2. Properties (Zero-boxing compiled property setter invocation)
                foreach (var prop in currentType.GetProperties(flags))
                {
                    if (Attribute.IsDefined(prop, typeof(InjectAttribute)) && prop.CanWrite)
                    {
                        var setter = prop.GetSetMethod(true);
                        if (setter != null)
                        {
                            var callResolve = Expression.Call(resolverParam, resolveMethod, Expression.Constant(prop.PropertyType));
                            var convertedVal = Expression.Convert(callResolve, prop.PropertyType);
                            expressions.Add(Expression.Call(typedTarget, setter, convertedVal));
                            injectionCount++;
                        }
                    }
                }

                // 3. Methods (Zero-array-allocation compiled method invocation)
                foreach (var method in currentType.GetMethods(flags))
                {
                    if (Attribute.IsDefined(method, typeof(InjectAttribute)))
                    {
                        var paramInfos = method.GetParameters();
                        var argExprs = new Expression[paramInfos.Length];
                        for (int i = 0; i < paramInfos.Length; i++)
                        {
                            var callResolve = Expression.Call(resolverParam, resolveMethod, Expression.Constant(paramInfos[i].ParameterType));
                            argExprs[i] = Expression.Convert(callResolve, paramInfos[i].ParameterType);
                        }
                        expressions.Add(Expression.Call(typedTarget, method, argExprs));
                        injectionCount++;
                    }
                }

                currentType = currentType.BaseType;
            }

            if (injectionCount == 0)
            {
                return null;
            }

            var block = Expression.Block(new[] { typedTarget }, expressions);
            var lambda = Expression.Lambda<Action<IResolver, object>>(block, resolverParam, targetParam);
            return lambda.Compile();
        }

        /// <summary>
        /// Clears all cached injector delegates (useful for unit testing or domain reload).
        /// </summary>
        public static void ClearCache()
        {
            lock (_lock)
            {
                _injectors.Clear();
            }
        }
    }
}
