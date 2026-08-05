using System;
using System.Collections.Generic;

namespace PirexZenith
{
    /// <summary>
    /// Builder used to register service bindings before creating an immutable DIContainer.
    /// </summary>
    public class ContainerBuilder
    {
        private readonly List<ServiceDescriptor> _descriptors = new List<ServiceDescriptor>();
        private DIContainer _parent;

        /// <summary>
        /// Assigns a parent container. Bindings in the current container will override matching parent bindings.
        /// </summary>
        public ContainerBuilder WithParent(DIContainer parent)
        {
            _parent = parent;
            return this;
        }

        /// <summary>
        /// Registers a Singleton mapping a concrete type to a service interface.
        /// </summary>
        public ContainerBuilder RegisterSingleton<TConcrete, TService>() where TConcrete : class, TService
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TService), typeof(TConcrete), Lifetime.Singleton));
            return this;
        }

        /// <summary>
        /// Registers a Singleton for a concrete type to itself.
        /// </summary>
        public ContainerBuilder RegisterSingleton<TConcrete>() where TConcrete : class
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TConcrete), typeof(TConcrete), Lifetime.Singleton));
            return this;
        }

        /// <summary>
        /// Registers a Singleton using runtime Types.
        /// </summary>
        public ContainerBuilder RegisterSingleton(Type concreteType, Type serviceType)
        {
            _descriptors.Add(new ServiceDescriptor(serviceType, concreteType, Lifetime.Singleton));
            return this;
        }

        /// <summary>
        /// Registers a Pure C# System decoupled from MonoBehaviour.
        /// Automatically registers the system type and any implemented lifecycle interfaces (IInitializable, ITickable, IFixedTickable, ILateTickable, IDisposable),
        /// ensuring only a single shared Singleton instance is created in the container.
        /// </summary>
        public ContainerBuilder RegisterSystem<TSystem>() where TSystem : class
        {
            Type type = typeof(TSystem);
            RegisterSingleton<TSystem>();

            if (typeof(IInitializable).IsAssignableFrom(type))
            {
                RegisterSingleton(type, typeof(IInitializable));
            }
            if (typeof(ITickable).IsAssignableFrom(type))
            {
                RegisterSingleton(type, typeof(ITickable));
            }
            if (typeof(IFixedTickable).IsAssignableFrom(type))
            {
                RegisterSingleton(type, typeof(IFixedTickable));
            }
            if (typeof(ILateTickable).IsAssignableFrom(type))
            {
                RegisterSingleton(type, typeof(ILateTickable));
            }
            if (typeof(IDisposable).IsAssignableFrom(type))
            {
                RegisterSingleton(type, typeof(IDisposable));
            }

            return this;
        }

        /// <summary>
        /// Registers a Transient mapping a concrete type to a service interface.
        /// </summary>
        public ContainerBuilder RegisterTransient<TConcrete, TService>() where TConcrete : class, TService
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TService), typeof(TConcrete), Lifetime.Transient));
            return this;
        }

        /// <summary>
        /// Registers a Transient for a concrete type to itself.
        /// </summary>
        public ContainerBuilder RegisterTransient<TConcrete>() where TConcrete : class
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TConcrete), typeof(TConcrete), Lifetime.Transient));
            return this;
        }

        /// <summary>
        /// Registers an existing pre-instantiated instance.
        /// </summary>
        public ContainerBuilder RegisterInstance<TService>(TService instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _descriptors.Add(new ServiceDescriptor(typeof(TService), instance));
            return this;
        }

        private readonly List<Action<IResolver>> _buildCallbacks = new List<Action<IResolver>>();

        /// <summary>
        /// Registers a service created by a custom factory delegate.
        /// </summary>
        public ContainerBuilder RegisterFactory<TService>(Func<IResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _descriptors.Add(new ServiceDescriptor(typeof(TService), resolver => factory(resolver), lifetime));
            return this;
        }

        /// <summary>
        /// Registers a callback to be invoked immediately after the DIContainer is built (VContainer compatibility).
        /// </summary>
        public ContainerBuilder RegisterBuildCallback(Action<IResolver> callback)
        {
            if (callback != null)
            {
                _buildCallbacks.Add(callback);
            }
            return this;
        }

        /// <summary>
        /// Builds and returns an immutable DIContainer.
        /// </summary>
        public DIContainer Build()
        {
            var container = new DIContainer(_descriptors, _parent);
            for (int i = 0; i < _buildCallbacks.Count; i++)
            {
                _buildCallbacks[i]?.Invoke(container);
            }
            return container;
        }
    }
}
