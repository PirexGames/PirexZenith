using PirexZenith.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PirexZenith.Editor
{
    /// <summary>
    /// Developer utility menu in Unity Editor.
    /// </summary>
    public static class PirexZenithMenu
    {
        [MenuItem("Tools/PirexZenith/Bake Current Scene", false, 1)]
        public static void BakeCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int count = DIEditorScanner.BakeScene(scene);
            stopwatch.Stop();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[PirexZenith] Manual Bake completed! Found {count} target(s) with [Inject] attribute in scene '{scene.name}' ({stopwatch.ElapsedMilliseconds} ms).");
        }

        [MenuItem("Tools/PirexZenith/Clear Baked Data (Test Fallback)", false, 2)]
        public static void ClearBakedData()
        {
            Scene scene = SceneManager.GetActiveScene();
            var scopes = Object.FindObjectsOfType<LifetimeScope>(true);
            foreach (var scope in scopes)
            {
                scope.bakedTargets.Clear();
                EditorUtility.SetDirty(scope);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[PirexZenith] Cleared bake data from {scopes.Length} LifetimeScope(s). Application will fall back to runtime scanning.");
        }

        [MenuItem("Tools/PirexZenith/Clear Editor Scan Cache", false, 3)]
        public static void ClearScanCache()
        {
            DIEditorScanner.ClearScanCache();
            Debug.Log("[PirexZenith] Cleared Editor Type scanning cache.");
        }

        [MenuItem("Tools/PirexZenith/Bake All Prefabs in Project", false, 4)]
        public static void BakeAllPrefabs()
        {
            int count = PrefabBakingSystem.BakeAllPrefabsInProject();
            Debug.Log($"[PirexZenith] Automatically inspected and attached DIPrefabCache to {count} Prefab(s) containing [Inject] attribute across the project!");
        }
    }
}
