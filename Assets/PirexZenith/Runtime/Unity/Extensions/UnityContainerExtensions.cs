using System.Collections.Generic;
using UnityEngine;

namespace PirexZenith
{
    /// <summary>
    /// Extension methods for DIContainer when interacting with Unity objects (e.g., Prefab instantiation).
    /// </summary>
    public static class UnityContainerExtensions
    {
        /// <summary>
        /// Instantiates a Prefab GameObject and automatically injects dependencies into all child MonoBehaviours.
        /// </summary>
        public static GameObject Instantiate(this DIContainer container, GameObject prefab, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            if (container == null) throw new System.ArgumentNullException(nameof(container));
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

            GameObject instance = Object.Instantiate(prefab, parent, instantiateInWorldSpace);
            InjectGameObject(container, instance);
            return instance;
        }

        /// <summary>
        /// Instantiates a Prefab GameObject at a specified position and rotation, automatically injecting dependencies into all child MonoBehaviours.
        /// </summary>
        public static GameObject Instantiate(this DIContainer container, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (container == null) throw new System.ArgumentNullException(nameof(container));
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

            GameObject instance = Object.Instantiate(prefab, position, rotation, parent);
            InjectGameObject(container, instance);
            return instance;
        }

        /// <summary>
        /// Instantiates a Component Prefab at a specified position and rotation, automatically injecting dependencies into all child MonoBehaviours.
        /// </summary>
        public static T Instantiate<T>(this DIContainer container, T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            if (container == null) throw new System.ArgumentNullException(nameof(container));
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

            T instance = Object.Instantiate(prefab, position, rotation, parent);
            InjectGameObject(container, instance.gameObject);
            return instance;
        }

        /// <summary>
        /// Instantiates a Component Prefab and automatically injects dependencies into all child MonoBehaviours.
        /// </summary>
        public static T Instantiate<T>(this DIContainer container, T prefab, Transform parent = null, bool instantiateInWorldSpace = false) where T : Component
        {
            if (container == null) throw new System.ArgumentNullException(nameof(container));
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

            T instance = Object.Instantiate(prefab, parent, instantiateInWorldSpace);
            InjectGameObject(container, instance.gameObject);
            return instance;
        }

        internal static void InjectGameObject(DIContainer container, GameObject instance)
        {
            if (instance == null) return;

            // If the instance contains its own LifetimeScope, let that scope manage its own container and injections.
            if (instance.GetComponentInChildren<LifetimeScope>(true) != null)
            {
                return;
            }

            // Check DIPrefabCache first for ahead-of-time baked target list
            if (instance.TryGetComponent<DIPrefabCache>(out var cache) && cache.CachedTargets != null && cache.CachedTargets.Count > 0)
            {
                if (cache.IsInjected) return;

                var targets = cache.CachedTargets;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != null)
                    {
                        container.Inject(targets[i]);
                    }
                }

                cache.IsInjected = true;
            }
            else
            {
                // Fallback: If no DIPrefabCache is present, scan all child MonoBehaviours at runtime
                var monos = instance.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < monos.Length; i++)
                {
                    if (monos[i] != null)
                    {
                        container.Inject(monos[i]);
                    }
                }
            }
        }

        #region VContainer Compatibility Extensions

        /// <summary>
        /// VContainer compatibility: Injects dependencies into a GameObject using IResolver.
        /// </summary>
        public static void InjectGameObject(this IResolver resolver, GameObject instance)
        {
            if (resolver is DIContainer container)
            {
                InjectGameObject(container, instance);
            }
        }

        /// <summary>
        /// VContainer compatibility: Instantiates a Prefab GameObject using IResolver and injects dependencies.
        /// </summary>
        public static GameObject Instantiate(this IResolver resolver, GameObject prefab, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            if (resolver is DIContainer container)
            {
                return container.Instantiate(prefab, parent, instantiateInWorldSpace);
            }
            throw new System.InvalidOperationException("[PirexZenith] Resolver must be a DIContainer instance to Instantiate GameObjects.");
        }

        /// <summary>
        /// VContainer compatibility: Instantiates a Component Prefab using IResolver and injects dependencies.
        /// </summary>
        public static T Instantiate<T>(this IResolver resolver, T prefab, Transform parent = null, bool instantiateInWorldSpace = false) where T : Component
        {
            if (resolver is DIContainer container)
            {
                return container.Instantiate(prefab, parent, instantiateInWorldSpace);
            }
            throw new System.InvalidOperationException("[PirexZenith] Resolver must be a DIContainer instance to Instantiate GameObjects.");
        }

        /// <summary>
        /// VContainer compatibility: Registers a Prefab so that when T is resolved, the Prefab is instantiated and injected.
        /// </summary>
        public static void RegisterComponentInNewPrefab<T>(this ContainerBuilder builder, T prefab, Lifetime lifetime = Lifetime.Singleton) where T : Component
        {
            if (builder == null) throw new System.ArgumentNullException(nameof(builder));
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

            builder.RegisterFactory(resolver => resolver.Instantiate(prefab), lifetime);
        }

        /// <summary>
        /// VContainer compatibility: Registers a GameObject Prefab so that when resolved, the Prefab is instantiated and injected.
        /// </summary>
        public static void RegisterComponentInNewPrefab(this ContainerBuilder builder, GameObject prefab, Lifetime lifetime = Lifetime.Singleton)
        {
            if (builder == null) throw new System.ArgumentNullException(nameof(builder));
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

            builder.RegisterFactory(resolver => resolver.Instantiate(prefab), lifetime);
        }

        #endregion
    }
}
