using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace PirexZenith
{
    /// <summary>
    /// Immutable Dependency Injection container created by ContainerBuilder.
    /// Manages instance resolution, lifetime scopes, and dependency injection.
    /// </summary>
    public class DIContainer : IResolver, IDisposable
    {
        private readonly Dictionary<Type, List<ServiceDescriptor>> _bindings;
        private readonly Dictionary<ServiceDescriptor, object> _singletonInstances;
        private readonly Dictionary<Type, object> _sharedSingletonsByType;
        private readonly HashSet<Type> _resolvingTypes;
        private readonly ConcurrentDictionary<Type, Func<object>> _fastResolvers;
        private readonly ConcurrentDictionary<Type, Func<object>[]> _fastMultiResolvers;
        private readonly object _lock = new object();
        private bool _disposed;

        /// <summary>
        /// Parent container (if any). Bindings in the current container override matching parent bindings.
        /// </summary>
        public DIContainer Parent { get; private set; }

        internal DIContainer(IEnumerable<ServiceDescriptor> descriptors, DIContainer parent = null)
        {
            Parent = parent;
            _bindings = new Dictionary<Type, List<ServiceDescriptor>>();
            _singletonInstances = new Dictionary<ServiceDescriptor, object>();
            _sharedSingletonsByType = new Dictionary<Type, object>();
            _resolvingTypes = new HashSet<Type>();
            _fastResolvers = new ConcurrentDictionary<Type, Func<object>>();
            _fastMultiResolvers = new ConcurrentDictionary<Type, Func<object>[]>();

            foreach (var descriptor in descriptors)
            {
                if (!_bindings.TryGetValue(descriptor.ServiceType, out var list))
                {
                    list = new List<ServiceDescriptor>();
                    _bindings[descriptor.ServiceType] = list;
                }
                list.Add(descriptor);

                if (descriptor.Lifetime == Lifetime.Instance && descriptor.ImplementationInstance != null)
                {
                    _singletonInstances[descriptor] = descriptor.ImplementationInstance;
                    _sharedSingletonsByType[descriptor.ImplementationType] = descriptor.ImplementationInstance;
                }
            }

            // Tá»± Ä‘Äƒng kÃ½ IResolver vÃ  DIContainer trá» vá» chÃ­nh container nÃ y
            var selfDescriptor1 = new ServiceDescriptor(typeof(IResolver), this);
            var selfDescriptor2 = new ServiceDescriptor(typeof(DIContainer), this);
            _bindings[typeof(IResolver)] = new List<ServiceDescriptor> { selfDescriptor1 };
            _bindings[typeof(DIContainer)] = new List<ServiceDescriptor> { selfDescriptor2 };
            _singletonInstances[selfDescriptor1] = this;
            _singletonInstances[selfDescriptor2] = this;
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (!TryResolve(type, out var result))
            {
                throw new DIException($"[PirexZenith] KhÃ´ng tÃ¬m tháº¥y Binding Ä‘Æ°á»£c Ä‘Äƒng kÃ½ cho Type: {type.FullName}");
            }
            return result;
        }

        public bool TryResolve<T>(out T result)
        {
            if (TryResolve(typeof(T), out var res) && res is T typedRes)
            {
                result = typedRes;
                return true;
            }
            result = default;
            return false;
        }

        public bool TryResolve(Type type, out object result)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DIContainer));

            if (_fastResolvers.TryGetValue(type, out var resolver))
            {
                result = resolver();
                return true;
            }

            return TryResolveAndCacheFastPath(type, out result);
        }

        private bool TryResolveAndCacheFastPath(Type type, out object result)
        {
            lock (_lock)
            {
                if (_bindings.TryGetValue(type, out var list) && list.Count > 0)
                {
                    var descriptor = list[list.Count - 1]; // Láº¥y binding cuá»‘i cÃ¹ng (máº·c Ä‘á»‹nh)
                    if (descriptor.Lifetime == Lifetime.Singleton || descriptor.Lifetime == Lifetime.Instance)
                    {
                        var instance = ResolveDescriptor(descriptor);
                        _fastResolvers[type] = () => instance;
                        result = instance;
                        return true;
                    }
                    else
                    {
                        var factory = BuildFastTransientResolver(descriptor);
                        _fastResolvers[type] = factory;
                        result = factory();
                        return true;
                    }
                }
            }

            if (Parent != null && Parent.TryResolve(type, out result))
            {
                return true;
            }

            result = null;
            return false;
        }

        private Func<object> BuildFastTransientResolver(ServiceDescriptor descriptor)
        {
            if (descriptor.Factory != null)
            {
                var factory = descriptor.Factory;
                return () =>
                {
                    var obj = factory(this);
                    if (InjectorCache.RequiresInjection(obj.GetType())) Inject(obj);
                    return obj;
                };
            }

            var type = descriptor.ImplementationType;

            if (PirexZenithAOTCache.TryGetFactory(type, out var aotFactory))
            {
                return () =>
                {
                    var obj = aotFactory(this);
                    if (InjectorCache.RequiresInjection(obj.GetType())) Inject(obj);
                    return obj;
                };
            }

            var cachedCtor = AnalyzeConstructor(type);

            if (cachedCtor.IsDefaultConstructor)
            {
                var newExpr = Expression.New(cachedCtor.Constructor != null ? cachedCtor.Constructor : type.GetConstructor(Type.EmptyTypes));
                var lambda = Expression.Lambda<Func<object>>(newExpr);
                var compiled = lambda.Compile();
                if (InjectorCache.RequiresInjection(type))
                {
                    return () =>
                    {
                        var obj = compiled();
                        Inject(obj);
                        return obj;
                    };
                }
                return compiled;
            }

            var paramTypes = cachedCtor.ParameterTypes;
            var resolveMethod = typeof(DIContainer).GetMethod(nameof(Resolve), new[] { typeof(Type) });
            var containerConstant = Expression.Constant(this);

            var argsExpr = new Expression[paramTypes.Length];
            for (int i = 0; i < paramTypes.Length; i++)
            {
                var callResolve = Expression.Call(
                    containerConstant,
                    resolveMethod,
                    Expression.Constant(paramTypes[i])
                );
                argsExpr[i] = Expression.Convert(callResolve, paramTypes[i]);
            }

            var newObjExpr = Expression.New(cachedCtor.Constructor, argsExpr);
            var lambdaFunc = Expression.Lambda<Func<object>>(newObjExpr);
            var compiledFunc = lambdaFunc.Compile();

            if (InjectorCache.RequiresInjection(type))
            {
                return () =>
                {
                    var obj = compiledFunc();
                    Inject(obj);
                    return obj;
                };
            }

            return compiledFunc;
        }

        public List<T> ResolveAll<T>()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DIContainer));

            var type = typeof(T);
            if (!_fastMultiResolvers.TryGetValue(type, out var resolvers))
            {
                resolvers = BuildFastMultiResolvers(type);
            }

            var result = new List<T>(resolvers.Length);
            for (int i = 0; i < resolvers.Length; i++)
            {
                result.Add((T)resolvers[i]());
            }
            return result;
        }

        public List<object> ResolveAll(Type type)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DIContainer));

            if (!_fastMultiResolvers.TryGetValue(type, out var resolvers))
            {
                resolvers = BuildFastMultiResolvers(type);
            }

            var result = new List<object>(resolvers.Length);
            for (int i = 0; i < resolvers.Length; i++)
            {
                result.Add(resolvers[i]());
            }
            return result;
        }

        private Func<object>[] BuildFastMultiResolvers(Type type)
        {
            lock (_lock)
            {
                if (_fastMultiResolvers.TryGetValue(type, out var existing))
                {
                    return existing;
                }

                var descriptorList = new List<ServiceDescriptor>();
                if (_bindings.TryGetValue(type, out var list))
                {
                    descriptorList.AddRange(list);
                }

                var currentParent = Parent;
                while (currentParent != null)
                {
                    lock (currentParent._lock)
                    {
                        if (currentParent._bindings.TryGetValue(type, out var parentList))
                        {
                            descriptorList.AddRange(parentList);
                        }
                    }
                    currentParent = currentParent.Parent;
                }

                var resolvers = new Func<object>[descriptorList.Count];
                for (int i = 0; i < descriptorList.Count; i++)
                {
                    var descriptor = descriptorList[i];
                    if (descriptor.Lifetime == Lifetime.Singleton || descriptor.Lifetime == Lifetime.Instance)
                    {
                        var desc = descriptor;
                        var instance = ResolveDescriptor(desc);
                        resolvers[i] = () => instance;
                    }
                    else
                    {
                        resolvers[i] = BuildFastTransientResolver(descriptor);
                    }
                }

                _fastMultiResolvers[type] = resolvers;
                return resolvers;
            }
        }

        public void Inject(object target)
        {
            if (target == null || _disposed) return;
            InjectorCache.Inject(this, target);
        }

        private object ResolveDescriptor(ServiceDescriptor descriptor)
        {
            if (descriptor.Lifetime == Lifetime.Singleton || descriptor.Lifetime == Lifetime.Instance)
            {
                if (_singletonInstances.TryGetValue(descriptor, out var instance))
                {
                    return instance;
                }
                // Náº¿u cÃ¹ng má»™t ImplementationType (khÃ´ng dÃ¹ng Factory riÃªng) Ä‘Ã£ táº¡o Singleton trÆ°á»›c Ä‘Ã³, chia sáº» chung instance!
                if (descriptor.Factory == null && _sharedSingletonsByType.TryGetValue(descriptor.ImplementationType, out var shared))
                {
                    _singletonInstances[descriptor] = shared;
                    return shared;
                }
            }

            if (!_resolvingTypes.Add(descriptor.ServiceType))
            {
                throw new DIException($"[PirexZenith] PhÃ¡t hiá»‡n phá»¥ thuá»™c vÃ²ng trÃ²n (Circular Dependency) khi Resolve Type: {descriptor.ServiceType.FullName}");
            }

            try
            {
                object instance;
                if (descriptor.Factory != null)
                {
                    instance = descriptor.Factory(this);
                }
                else
                {
                    instance = CreateInstance(descriptor.ImplementationType);
                }

                if (descriptor.Lifetime == Lifetime.Singleton)
                {
                    _singletonInstances[descriptor] = instance;
                    if (descriptor.Factory == null && !_sharedSingletonsByType.ContainsKey(descriptor.ImplementationType))
                    {
                        _sharedSingletonsByType[descriptor.ImplementationType] = instance;
                    }
                }

                if (InjectorCache.RequiresInjection(instance.GetType()))
                {
                    Inject(instance);
                }
                return instance;
            }
            finally
            {
                _resolvingTypes.Remove(descriptor.ServiceType);
            }
        }

        private struct CachedConstructorInfo
        {
            public bool IsDefaultConstructor;
            public ConstructorInfo Constructor;
            public Type[] ParameterTypes;
        }

        private static readonly Dictionary<Type, CachedConstructorInfo> _constructorCache = new Dictionary<Type, CachedConstructorInfo>();

        public static void ClearConstructorCache()
        {
            lock (_constructorCache)
            {
                _constructorCache.Clear();
            }
        }

        private object CreateInstance(Type type)
        {
            if (PirexZenithAOTCache.TryGetFactory(type, out var aotFactory))
            {
                return aotFactory(this);
            }

            CachedConstructorInfo cached;
            lock (_constructorCache)
            {
                if (!_constructorCache.TryGetValue(type, out cached))
                {
                    cached = AnalyzeConstructor(type);
                    _constructorCache[type] = cached;
                }
            }

            if (cached.IsDefaultConstructor)
            {
                return Activator.CreateInstance(type);
            }

            var parameters = cached.ParameterTypes;
            var args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = Resolve(parameters[i]);
            }

            return cached.Constructor.Invoke(args);
        }

        private CachedConstructorInfo AnalyzeConstructor(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            ConstructorInfo chosenConstructor = null;

            // Æ¯u tiÃªn constructor cÃ³ Ä‘Ã¡nh dáº¥u [Inject]
            foreach (var ctor in constructors)
            {
                if (Attribute.IsDefined(ctor, typeof(InjectAttribute)))
                {
                    chosenConstructor = ctor;
                    break;
                }
            }

            // Náº¿u khÃ´ng cÃ³, chá»n constructor cÃ³ nhiá»u tham sá»‘ nháº¥t (Zero LINQ / Zero GC)
            if (chosenConstructor == null && constructors.Length > 0)
            {
                int maxParams = -1;
                for (int i = 0; i < constructors.Length; i++)
                {
                    int paramCount = constructors[i].GetParameters().Length;
                    if (paramCount > maxParams)
                    {
                        maxParams = paramCount;
                        chosenConstructor = constructors[i];
                    }
                }
            }

            if (chosenConstructor == null || chosenConstructor.GetParameters().Length == 0)
            {
                return new CachedConstructorInfo
                {
                    IsDefaultConstructor = true,
                    Constructor = chosenConstructor,
                    ParameterTypes = Array.Empty<Type>()
                };
            }

            var paramInfos = chosenConstructor.GetParameters();
            var paramTypes = new Type[paramInfos.Length];
            for (int i = 0; i < paramInfos.Length; i++)
            {
                paramTypes[i] = paramInfos[i].ParameterType;
            }

            return new CachedConstructorInfo
            {
                IsDefaultConstructor = false,
                Constructor = chosenConstructor,
                ParameterTypes = paramTypes
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_lock)
            {
                var disposedSet = new HashSet<object>();
                foreach (var instance in _singletonInstances.Values)
                {
                    if (instance is IDisposable disposable && !ReferenceEquals(instance, this) && disposedSet.Add(instance))
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine($"[PirexZenith] Error while disposing service '{instance.GetType().Name}': {ex}");
                        }
                    }
                }
                _singletonInstances.Clear();
                _sharedSingletonsByType.Clear();
                _bindings.Clear();
                _fastResolvers.Clear();
                _fastMultiResolvers.Clear();
            }
        }
    }
}
