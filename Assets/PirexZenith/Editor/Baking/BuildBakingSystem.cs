using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PirexZenith.Editor
{
    /// <summary>
    /// Ensures all Scenes are baked prior to generating a game build, delivering O(1) resolution performance in production builds.
    /// </summary>
    public class BuildBakingSystem : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null)
            {
                // Called from Editor PlayMode or without a build report -> skip or perform quick validation
                return;
            }

            int count = DIEditorScanner.BakeScene(scene);
            if (count > 0)
            {
                Debug.Log($"[PirexZenith BuildBaking] Baked {count} target(s) into LifetimeScope of scene '{scene.name}' before build.");
            }
        }
    }
}
