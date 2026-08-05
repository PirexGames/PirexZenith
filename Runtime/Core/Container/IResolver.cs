using System;
using System.Collections.Generic;

namespace PirexZenith
{
    /// <summary>
    /// Core interface for dependency resolution and injection.
    /// </summary>
    public interface IResolver
    {
        /// <summary>
        /// Resolves a service instance registered for type T.
        /// </summary>
        T Resolve<T>();

        /// <summary>
        /// Resolves a service instance registered for the specified runtime Type.
        /// </summary>
        object Resolve(Type type);

        /// <summary>
        /// Attempts to resolve a service instance of type T. Returns false if not registered.
        /// </summary>
        bool TryResolve<T>(out T result);

        /// <summary>
        /// Attempts to resolve a service instance of the specified runtime Type. Returns false if not registered.
        /// </summary>
        bool TryResolve(Type type, out object result);

        /// <summary>
        /// Resolves all service instances registered for type T.
        /// </summary>
        List<T> ResolveAll<T>();

        /// <summary>
        /// Resolves all service instances registered for the specified runtime Type.
        /// </summary>
        List<object> ResolveAll(Type type);

        /// <summary>
        /// Performs dependency injection on an existing target instance.
        /// </summary>
        void Inject(object target);
    }
}
