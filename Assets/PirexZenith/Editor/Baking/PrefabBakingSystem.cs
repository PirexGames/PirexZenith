using System;
using System.Collections.Generic;
using PirexZenith;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PirexZenith.Editor
{
    /// <summary>
    /// Auto-Baking System for Prefabs: Automatically listens for prefab save events (Prefab Stage or Project Asset),
    /// discovers scripts marked with [Inject], attaches <see cref="DIPrefabCache"/>, and pre-populates target references.
    /// Eliminates the need for manual component setup.
    /// </summary>
    [InitializeOnLoad]
    public class PrefabBakingSystem : AssetPostprocessor
    {
        static PrefabBakingSystem()
        {
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        private static void OnPrefabSaving(GameObject prefabRoot)
        {
            if (prefabRoot == null) return;
            if (BakePrefab(prefabRoot, out int count, out bool added))
            {
                Debug.Log($"[PirexZenith] Auto-Bake Prefab Stage '{prefabRoot.name}': {(added ? "Automatically added DIPrefabCache and " : "")}cached {count} target(s) with [Inject].");
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    string prefabPath = path;
                    EditorApplication.delayCall += () =>
                    {
                        ProcessPrefabAtPath(prefabPath);
                    };
                }
            }
        }

        private static void ProcessPrefabAtPath(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            if (BakePrefab(prefab, out int count, out bool added))
            {
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"[PirexZenith] Auto-Bake Prefab Asset '{prefab.name}': {(added ? "Automatically added DIPrefabCache and " : "")}cached {count} target(s) with [Inject] ({path}).");
            }
        }

        /// <summary>
        /// Scans and automatically bakes a Prefab.
        /// If the Prefab has a LifetimeScope attached, bakes targets into LifetimeScope.bakedTargets.
        /// Otherwise, attaches DIPrefabCache and bakes targets into DIPrefabCache.
        /// Returns true if the Prefab was modified and requires saving.
        /// </summary>
        public static bool BakePrefab(GameObject prefabRoot, out int count, out bool componentAdded)
        {
            count = 0;
            componentAdded = false;

            if (prefabRoot == null) return false;

            // 1. Scan all MonoBehaviours with [Inject] attribute in the Prefab hierarchy
            var allMonos = prefabRoot.GetComponentsInChildren<MonoBehaviour>(true);
            var injectableTargets = new List<MonoBehaviour>();

            foreach (var mono in allMonos)
            {
                if (mono == null || mono is DIPrefabCache || mono is LifetimeScope) continue;
                if (DIEditorScanner.ScanForInjectAttribute(mono.GetType()))
                {
                    injectableTargets.Add(mono);
                }
            }

            // 2. If the prefab has a LifetimeScope attached, it acts as a Prefab Scope (managing its own DI lifecycle).
            // Bake directly into LifetimeScope.bakedTargets!
            var scope = prefabRoot.GetComponentInChildren<LifetimeScope>(true);
            if (scope != null)
            {
                bool modified = false;

                // Remove any existing DIPrefabCache if present, since LifetimeScope manages DI here
                DIPrefabCache existingCache = prefabRoot.GetComponent<DIPrefabCache>();
                if (existingCache != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingCache, true);
                    modified = true;
                }

                if (!IsListIdentical(scope.bakedTargets, injectableTargets))
                {
                    scope.bakedTargets = new List<MonoBehaviour>(injectableTargets);
                    EditorUtility.SetDirty(scope);
                    modified = true;
                }

                if (modified)
                {
                    EditorUtility.SetDirty(prefabRoot);
                    count = injectableTargets.Count;
                    return true;
                }
                return false;
            }

            // If no script has an [Inject] attribute and no LifetimeScope, do nothing
            if (injectableTargets.Count == 0)
            {
                return false;
            }

            count = injectableTargets.Count;

            // 3. Otherwise, automatically attach DIPrefabCache or InjectionScope based on preference
            int autoBakeMode = EditorPrefs.GetInt(PirexZenithEditorWindow.PREF_AUTOBAKE_MODE, 0);

            if (autoBakeMode == 1) // InjectionScope mode
            {
                InjectionScope injectionScope = prefabRoot.GetComponent<InjectionScope>();
                if (injectionScope == null)
                {
                    injectionScope = prefabRoot.AddComponent<InjectionScope>();
                    componentAdded = true;
                }

                if (!componentAdded && IsListIdentical(injectionScope.bakedTargets, injectableTargets))
                {
                    return false;
                }

                injectionScope.bakedTargets = new List<MonoBehaviour>(injectableTargets);
                EditorUtility.SetDirty(injectionScope);
                EditorUtility.SetDirty(prefabRoot);
                return true;
            }
            else // DIPrefabCache mode
            {
                DIPrefabCache cache = prefabRoot.GetComponent<DIPrefabCache>();
                if (cache == null)
                {
                    cache = prefabRoot.AddComponent<DIPrefabCache>();
                    componentAdded = true;
                }

                // 4. Verify whether the current cache is already identical (prevents infinite dirty loops)
                if (!componentAdded && IsListIdentical(cache.CachedTargets, injectableTargets))
                {
                    return false;
                }

                // 5. Update the target list in cache
                cache.SetCachedTargets(injectableTargets);
                EditorUtility.SetDirty(cache);
                EditorUtility.SetDirty(prefabRoot);
                return true;
            }
        }

        private static bool IsListIdentical(List<MonoBehaviour> current, List<MonoBehaviour> target)
        {
            if (current == null && target == null) return true;
            if (current == null || target == null) return false;
            if (current.Count != target.Count) return false;
            for (int i = 0; i < current.Count; i++)
            {
                if (current[i] != target[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Scans all Prefabs in the project and automatically attaches DIPrefabCache to Prefabs containing [Inject] attributes.
        /// </summary>
        public static int BakeAllPrefabsInProject()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int bakedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    if (BakePrefab(prefab, out int count, out bool added))
                    {
                        PrefabUtility.SavePrefabAsset(prefab);
                        bakedCount++;
                    }
                }
            }

            return bakedCount;
        }
    }
}
