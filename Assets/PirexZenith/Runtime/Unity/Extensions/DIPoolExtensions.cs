#if PIREX_REXPOOL_ENABLED
using PirexGames.RexPool;
#endif
using PirexZenith.Core;
using UnityEngine;

namespace PirexZenith.Unity
{
    /// <summary>
    /// Extension methods integrating RexPoolManager object pooling with DIContainer dependency injection.
    /// Methods are ALWAYS available on <see cref="DIContainer"/> unconditionally.
    /// When 'com.pirexgames.rexpool' is installed, pooling is handled via RexPoolManager (O(1), 0-GC allocation).
    /// Otherwise, methods gracefully fall back to standard Unity <see cref="UnityEngine.Object.Instantiate"/> with identical behavior.
    /// </summary>
    public static class DIPoolExtensions
    {
        /// <summary>
        /// Take a GameObject from the pool (or instantiate if RexPool is absent), automatically injecting dependencies.
        /// </summary>
        public static GameObject Take(
            this DIContainer container,
            GameObject prefab,
            bool activeObject = true)
        {
            return container.Take(prefab, Vector3.zero, Quaternion.identity, null, activeObject);
        }

        /// <summary>
        /// Take a GameObject from the pool at a specified position and rotation, automatically injecting dependencies.
        /// </summary>
        public static GameObject Take(
            this DIContainer container,
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            bool activeObject = true)
        {
            if (container == null) throw new System.ArgumentNullException(nameof(container));
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

#if PIREX_REXPOOL_ENABLED
            GameObject instance = RexPoolManager.TakeRPO(prefab, activeObject);
            Transform transform = instance.transform;
            if (parent != null)
            {
                transform.SetParent(parent, false);
            }
            transform.SetPositionAndRotation(position, rotation);
#else
            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            if (!activeObject && instance.activeSelf)
            {
                instance.SetActive(false);
            }
            else if (activeObject && !instance.activeSelf)
            {
                instance.SetActive(true);
            }
#endif
            // Injects dependencies only if this is a newly created instance (O(1) IsInjected guard in DIPrefabCache)
            UnityContainerExtensions.InjectGameObject(container, instance);
            return instance;
        }

        /// <summary>
        /// Take a GameObject from the pool under a parent Transform.
        /// </summary>
        public static GameObject Take(
            this DIContainer container,
            GameObject prefab,
            Transform parent,
            bool activeObject = true)
        {
            return container.Take(
                prefab,
                parent != null ? parent.position : Vector3.zero,
                parent != null ? parent.rotation : Quaternion.identity,
                parent,
                activeObject);
        }

        /// <summary>
        /// Core take — O(1) amortized when RexPool is present. Automatically injects dependencies into newly created instances.
        /// </summary>
        public static GameObject TakeRPO(
            this DIContainer container,
            GameObject prefab,
            bool activeObject = true)
        {
            return container.Take(prefab, Vector3.zero, Quaternion.identity, null, activeObject);
        }

        /// <summary>
        /// Core take at a specified position and rotation — O(1) amortized when RexPool is present.
        /// </summary>
        public static GameObject TakeRPO(
            this DIContainer container,
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            bool activeObject = true)
        {
            return container.Take(prefab, position, rotation, parent, activeObject);
        }

        /// <summary>
        /// Take a typed component from the pool (or instantiate if RexPool is absent).
        /// </summary>
        public static T Take<T>(
            this DIContainer container,
            GameObject prefab,
            bool activeObject = true) where T : MonoBehaviour
        {
            return container.Take<T>(prefab, Vector3.zero, Quaternion.identity, null, activeObject);
        }

        /// <summary>
        /// Take a typed component from the pool at a specified position and rotation.
        /// </summary>
        public static T Take<T>(
            this DIContainer container,
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            bool activeObject = true) where T : MonoBehaviour
        {
            if (container == null) throw new System.ArgumentNullException(nameof(container));
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

#if PIREX_REXPOOL_ENABLED
            GameObject goInstance = container.Take(prefab, position, rotation, parent, activeObject);
            if (goInstance.TryGetComponent<RexPoolObject>(out var rpo))
            {
                var cached = rpo.GetCachedComponent<T>();
                if (cached != null) return cached;
            }
            return goInstance.GetComponent<T>();
#else
            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            if (!activeObject && instance.activeSelf)
            {
                instance.SetActive(false);
            }
            else if (activeObject && !instance.activeSelf)
            {
                instance.SetActive(true);
            }
            UnityContainerExtensions.InjectGameObject(container, instance);
            return instance.GetComponent<T>();
#endif
        }

        /// <summary>
        /// Take a typed component from the pool under a parent Transform.
        /// </summary>
        public static T Take<T>(
            this DIContainer container,
            GameObject prefab,
            Transform parent,
            bool activeObject = true) where T : MonoBehaviour
        {
            return container.Take<T>(
                prefab,
                parent != null ? parent.position : Vector3.zero,
                parent != null ? parent.rotation : Quaternion.identity,
                parent,
                activeObject);
        }

        /// <summary>
        /// Take a typed component from the pool using a component prefab reference.
        /// </summary>
        public static T Take<T>(
            this DIContainer container,
            T prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            bool activeObject = true) where T : MonoBehaviour
        {
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));
            return container.Take<T>(prefab.gameObject, position, rotation, parent, activeObject);
        }

        /// <summary>
        /// Take a typed component from the pool using a component prefab reference under a parent Transform.
        /// </summary>
        public static T Take<T>(
            this DIContainer container,
            T prefab,
            Transform parent = null,
            bool activeObject = true) where T : MonoBehaviour
        {
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));
            return container.Take<T>(prefab.gameObject, parent, activeObject);
        }

        /// <summary>
        /// Pre-warm the pool with a number of inactive instances.
        /// If RexPool is not installed, this method performs a safe no-op.
        /// </summary>
        public static void Prepair(this DIContainer container, GameObject prefab, int amount)
        {
            if (prefab == null) return;
#if PIREX_REXPOOL_ENABLED
            RexPoolManager.Prepair(prefab, amount);
#endif
        }

        /// <summary>
        /// Destroy all pooled instances of this prefab and clear tracking data.
        /// If RexPool is not installed, this method performs a safe no-op.
        /// </summary>
        public static void CleanUp(this DIContainer container, GameObject prefab)
        {
            if (prefab == null) return;
#if PIREX_REXPOOL_ENABLED
            RexPoolManager.CleanUp(prefab);
#endif
        }
    }
}
