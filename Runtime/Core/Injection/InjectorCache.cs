using System;
using System.Collections.Generic;
using System.Reflection;

namespace PirexZenith.Core
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

                var injector = BuildReflectionInjector(type);
                _injectors[type] = injector;
                return injector;
            }
        }

        private static Action<IResolver, object> BuildReflectionInjector(Type type)
        {
            var fieldInjectors = new List<Action<IResolver, object>>();
            var propertyInjectors = new List<Action<IResolver, object>>();
            var methodInjectors = new List<Action<IResolver, object>>();

            Type currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                // 1. Fields
                foreach (var field in currentType.GetFields(flags))
                {
                    if (Attribute.IsDefined(field, typeof(InjectAttribute)))
                    {
                        var fieldInfo = field;
                        fieldInjectors.Add((resolver, obj) =>
                        {
                            var val = resolver.Resolve(fieldInfo.FieldType);
                            fieldInfo.SetValue(obj, val);
                        });
                    }
                }

                // 2. Properties
                foreach (var prop in currentType.GetProperties(flags))
                {
                    if (Attribute.IsDefined(prop, typeof(InjectAttribute)) && prop.CanWrite)
                    {
                        var propInfo = prop;
                        propertyInjectors.Add((resolver, obj) =>
                        {
                            var val = resolver.Resolve(propInfo.PropertyType);
                            propInfo.SetValue(obj, val);
                        });
                    }
                }

                // 3. Methods
                foreach (var method in currentType.GetMethods(flags))
                {
                    if (Attribute.IsDefined(method, typeof(InjectAttribute)))
                    {
                        var methodInfo = method;
                        var paramInfos = methodInfo.GetParameters();
                        methodInjectors.Add((resolver, obj) =>
                        {
                            var args = new object[paramInfos.Length];
                            for (int i = 0; i < paramInfos.Length; i++)
                            {
                                args[i] = resolver.Resolve(paramInfos[i].ParameterType);
                            }
                            methodInfo.Invoke(obj, args);
                        });
                    }
                }

                currentType = currentType.BaseType;
            }

            if (fieldInjectors.Count == 0 && propertyInjectors.Count == 0 && methodInjectors.Count == 0)
            {
                return null;
            }

            return (resolver, obj) =>
            {
                for (int i = 0; i < fieldInjectors.Count; i++) fieldInjectors[i](resolver, obj);
                for (int i = 0; i < propertyInjectors.Count; i++) propertyInjectors[i](resolver, obj);
                for (int i = 0; i < methodInjectors.Count; i++) methodInjectors[i](resolver, obj);
            };
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
