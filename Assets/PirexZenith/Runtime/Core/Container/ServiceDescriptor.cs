using System;

namespace PirexZenith.Core
{
    /// <summary>
    /// Describes a registered service binding in the container.
    /// </summary>
    public class ServiceDescriptor
    {
        public Type ServiceType { get; }
        public Type ImplementationType { get; }
        public Lifetime Lifetime { get; }
        public object ImplementationInstance { get; set; }
        public Func<IResolver, object> Factory { get; }

        public ServiceDescriptor(Type serviceType, Type implementationType, Lifetime lifetime)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
            Lifetime = lifetime;
        }

        public ServiceDescriptor(Type serviceType, object instance)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            ImplementationInstance = instance ?? throw new ArgumentNullException(nameof(instance));
            ImplementationType = instance.GetType();
            Lifetime = Lifetime.Instance;
        }

        public ServiceDescriptor(Type serviceType, Func<IResolver, object> factory, Lifetime lifetime)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            ImplementationType = serviceType;
            Lifetime = lifetime;
        }
    }
}
