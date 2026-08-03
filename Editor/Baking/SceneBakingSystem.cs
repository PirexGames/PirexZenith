using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PirexZenith.Editor
{
    /// <summary>
    /// Auto-Baking System: Listens for scene save events to automatically update bakedTargets lists in LifetimeScopes.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneBakingSystem
    {
        static SceneBakingSystem()
        {
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int count = DIEditorScanner.BakeScene(scene);
            stopwatch.Stop();

            if (count > 0)
            {
                Debug.Log($"[PirexZenith] Auto-Bake completed for scene '{scene.name}': Baked {count} target(s) into LifetimeScope in {stopwatch.ElapsedMilliseconds} ms.");
            }
        }
    }
}
