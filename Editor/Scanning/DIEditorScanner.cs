using System;
using System.Collections.Generic;
using System.Reflection;
using PirexZenith.Core;
using PirexZenith.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PirexZenith.Editor
{
    /// <summary>
    /// Editor scanner that inspects Types for [Inject] attributes using a high-speed cache.
    /// </summary>
    public static class DIEditorScanner
    {
        private static readonly Dictionary<Type, bool> _scanCache = new Dictionary<Type, bool>();

        /// <summary>
        /// Scans whether a class or any of its base classes declare any [Inject] attributes on Fields, Properties, or Methods.
        /// Uses caching to keep scan duration < 50ms during Scene saves.
        /// </summary>
        public static bool ScanForInjectAttribute(Type type)
        {
            if (type == null) return false;

            if (_scanCache.TryGetValue(type, out bool cachedResult))
            {
                return cachedResult;
            }

            bool hasInject = InternalScanForInjectAttribute(type);
            _scanCache[type] = hasInject;
            return hasInject;
        }

        private static bool InternalScanForInjectAttribute(Type type)
        {
            Type currentType = type;

            // Loop upwards through base types, stopping at MonoBehaviour or Object
            while (currentType != null && currentType != typeof(MonoBehaviour) && currentType != typeof(object))
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                // 1. Inspect Fields
                foreach (var field in currentType.GetFields(flags))
                {
                    if (Attribute.IsDefined(field, typeof(InjectAttribute))) return true;
                }

                // 2. Inspect Properties
                foreach (var prop in currentType.GetProperties(flags))
                {
                    if (Attribute.IsDefined(prop, typeof(InjectAttribute))) return true;
                }

                // 3. Inspect Methods
                foreach (var method in currentType.GetMethods(flags))
                {
                    if (Attribute.IsDefined(method, typeof(InjectAttribute))) return true;
                }

                // Move up to the base type
                currentType = currentType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Bakes the entire Scene: Scans all MonoBehaviours containing [Inject] and stores them in LifetimeScope.
        /// </summary>
        public static int BakeScene(Scene scene)
        {
            if (!scene.isLoaded) return 0;

            var rootObjects = scene.GetRootGameObjects();
            var allScopes = new List<LifetimeScope>();
            var allMonos = new List<MonoBehaviour>();

            foreach (var root in rootObjects)
            {
                allScopes.AddRange(root.GetComponentsInChildren<LifetimeScope>(true));
                allMonos.AddRange(root.GetComponentsInChildren<MonoBehaviour>(true));
            }

            var targetsToInject = new List<MonoBehaviour>();
            foreach (var mono in allMonos)
            {
                if (mono != null && mono is not LifetimeScope && mono is not DIPrefabCache && ScanForInjectAttribute(mono.GetType()))
                {
                    targetsToInject.Add(mono);
                }
            }

            // If scripts with [Inject] are detected but the scene lacks a LifetimeScope, automatically create one
            if (targetsToInject.Count > 0 && allScopes.Count == 0)
            {
                GameObject scopeGO = new GameObject("--- PIREX_DI_SCOPE ---");
                LifetimeScope newScope = scopeGO.AddComponent<LifetimeScope>();
                allScopes.Add(newScope);
                Debug.Log($"[PirexZenith] Automatically created GameObject '--- PIREX_DI_SCOPE ---' with LifetimeScope in scene '{scene.name}' because {targetsToInject.Count} script(s) with [Inject] were detected.");
            }

            if (allScopes.Count == 0)
            {
                return 0;
            }

            foreach (var scope in allScopes)
            {
                if (!AreListsEqual(scope.bakedTargets, targetsToInject))
                {
                    scope.bakedTargets = new List<MonoBehaviour>(targetsToInject);
                    EditorUtility.SetDirty(scope);
                }
            }

            return targetsToInject.Count;
        }

        private static bool AreListsEqual(List<MonoBehaviour> listA, List<MonoBehaviour> listB)
        {
            if (listA == null && listB == null) return true;
            if (listA == null || listB == null) return false;
            if (listA.Count != listB.Count) return false;

            for (int i = 0; i < listA.Count; i++)
            {
                if (listA[i] != listB[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Clears the Editor Type scan cache (useful during script recompilation or menu invocation).
        /// </summary>
        public static void ClearScanCache()
        {
            _scanCache.Clear();
        }
    }
}
