using System;
using System.Collections.Generic;

namespace PirexZenith.Core
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

            if (typeof(PirexZenith.Core.Lifecycle.IInitializable).IsAssignableFrom(type))
            {
                RegisterSingleton(type, typeof(PirexZenith.Core.Lifecycle.IInitializable));
            }
            if (typeof(PirexZenith.Core.Lifecycle.ITickable).IsAssignableFrom(type))
            {
                RegisterSingleton(type, typeof(PirexZenith.Core.Lifecycle.ITickable));
            }
            if (typeof(PirexZenith.Core.Lifecycle.IFixedTickable).IsAssignableFrom(type))
            {
                RegisterSingleton(type, typeof(PirexZenith.Core.Lifecycle.IFixedTickable));
            }
            if (typeof(PirexZenith.Core.Lifecycle.ILateTickable).IsAssignableFrom(type))
            {
                RegisterSingleton(type, typeof(PirexZenith.Core.Lifecycle.ILateTickable));
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
        /// Builds and returns an immutable DIContainer.
        /// </summary>
        public DIContainer Build()
        {
            var container = new DIContainer(_descriptors, _parent);
            return container;
        }
    }
}
