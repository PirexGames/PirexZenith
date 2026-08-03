using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace PirexZenith.Core
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

            // Tự đăng ký IResolver và DIContainer trỏ về chính container này
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
                throw new DIException($"[PirexZenith] Không tìm thấy Binding được đăng ký cho Type: {type.FullName}");
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
                    var descriptor = list[list.Count - 1]; // Lấy binding cuối cùng (mặc định)
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

        public IEnumerable<T> ResolveAll<T>()
        {
            return ResolveAll(typeof(T)).Cast<T>();
        }

        public IEnumerable<object> ResolveAll(Type type)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DIContainer));

            List<object> results = new List<object>();

            lock (_lock)
            {
                if (_bindings.TryGetValue(type, out var list))
                {
                    results.AddRange(list.Select(ResolveDescriptor));
                }
            }

            if (Parent != null)
            {
                results.AddRange(Parent.ResolveAll(type));
            }

            return results;
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
                // Nếu cùng một ImplementationType (không dùng Factory riêng) đã tạo Singleton trước đó, chia sẻ chung instance!
                if (descriptor.Factory == null && _sharedSingletonsByType.TryGetValue(descriptor.ImplementationType, out var shared))
                {
                    _singletonInstances[descriptor] = shared;
                    return shared;
                }
            }

            if (!_resolvingTypes.Add(descriptor.ServiceType))
            {
                throw new DIException($"[PirexZenith] Phát hiện phụ thuộc vòng tròn (Circular Dependency) khi Resolve Type: {descriptor.ServiceType.FullName}");
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

            // Ưu tiên constructor có đánh dấu [Inject]
            foreach (var ctor in constructors)
            {
                if (Attribute.IsDefined(ctor, typeof(InjectAttribute)))
                {
                    chosenConstructor = ctor;
                    break;
                }
            }

            // Nếu không có, chọn constructor có nhiều tham số nhất (Zero LINQ / Zero GC)
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
            }
        }
    }
}
