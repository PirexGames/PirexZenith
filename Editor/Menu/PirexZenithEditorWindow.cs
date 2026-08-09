using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using PirexZenith.Editor.Migration;

namespace PirexZenith.Editor
{
    public class PirexZenithEditorWindow : EditorWindow
    {
        private int _currentTab = 0;
        private readonly string[] _tabNames = { "Settings", "AOT Baker", "Utilities", "Migrator" };

        public const string PREF_AUTOBAKE_MODE = "PirexZenith_AutoBakeMode";

        [MenuItem("Tools/UnityPrime/PirexZenith/⚙️ Zenith Control Panel", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<PirexZenithEditorWindow>("Zenith Control Panel");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("PirexZenith Control Panel", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _currentTab = GUILayout.Toolbar(_currentTab, _tabNames, GUILayout.Height(30));
            EditorGUILayout.Space(15);

            switch (_currentTab)
            {
                case 0:
                    DrawSettingsTab();
                    break;
                case 1:
                    DrawAOTBakerTab();
                    break;
                case 2:
                    DrawUtilitiesTab();
                    break;
                case 3:
                    DrawMigratorTab();
                    break;
            }
        }

        private void DrawSettingsTab()
        {
            GUILayout.Label("Auto-Bake Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure which component is automatically attached to Prefabs or Scenes when an [Inject] attribute is detected during save.", 
                MessageType.Info);
            
            EditorGUILayout.Space(10);

            int currentMode = EditorPrefs.GetInt(PREF_AUTOBAKE_MODE, 0);
            
            EditorGUILayout.LabelField("Preferred Component for Auto-Bake:", EditorStyles.label);
            string[] options = { "0. DIPrefabCache (Lightweight Array Cache)", "1. InjectionScope (Full DI Scope, Isolated)" };
            
            int newMode = EditorGUILayout.Popup(currentMode, options);
            if (newMode != currentMode)
            {
                EditorPrefs.SetInt(PREF_AUTOBAKE_MODE, newMode);
            }

            EditorGUILayout.Space(15);
            if (newMode == 0)
            {
                EditorGUILayout.HelpBox("DIPrefabCache: A lightweight component that only caches the [Inject] targets as an array. It does not create a DI Container and relies on a parent LifetimeScope to inject it. Recommended for Prefabs.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("InjectionScope: A full DI Scope that inherits from LifetimeScope. It creates its own DI Container to inject itself and its children, but remains isolated (AutoRegisterToActiveScopes = false) so it doesn't become the parent of subsequent dynamic instantiations. Great for independent UI Views or Modules.", MessageType.None);
            }
        }

        private void DrawAOTBakerTab()
        {
            GUILayout.Label("AOT Code Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates static C# factory code for AOT / IL2CPP zero-reflection DI. " +
                "Run this before making an IL2CPP build for maximum performance.", 
                MessageType.Info);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("⚡ Bake AOT Code Generator", GUILayout.Height(40)))
            {
                PirexZenithAOTBaker.BakeAOTCode();
            }
        }

        private void DrawUtilitiesTab()
        {
            GUILayout.Label("Baking Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Bake Current Scene", GUILayout.Height(30)))
            {
                Scene scene = SceneManager.GetActiveScene();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int count = DIEditorScanner.BakeScene(scene);
                stopwatch.Stop();

                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[PirexZenith] Manual Bake completed! Found {count} target(s) with [Inject] attribute in scene '{scene.name}' ({stopwatch.ElapsedMilliseconds} ms).");
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Bake All Prefabs in Project", GUILayout.Height(30)))
            {
                int count = PrefabBakingSystem.BakeAllPrefabsInProject();
                int mode = EditorPrefs.GetInt(PREF_AUTOBAKE_MODE, 0);
                string componentName = mode == 0 ? "DIPrefabCache" : "InjectionScope";
                Debug.Log($"[PirexZenith] Automatically inspected and attached {componentName} to {count} Prefab(s) containing [Inject] attribute across the project!");
            }

            EditorGUILayout.Space(15);
            GUILayout.Label("Cache Management", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Clear Editor Scan Cache", GUILayout.Height(30)))
            {
                DIEditorScanner.ClearScanCache();
                Debug.Log("[PirexZenith] Cleared Editor Type scanning cache.");
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Clear Baked Data (Test Fallback)", GUILayout.Height(30)))
            {
                Scene scene = SceneManager.GetActiveScene();
                var scopes = FindObjectsOfType<LifetimeScope>(true);
                foreach (var scope in scopes)
                {
                    scope.bakedTargets.Clear();
                    EditorUtility.SetDirty(scope);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[PirexZenith] Cleared bake data from {scopes.Length} LifetimeScope(s). Application will fall back to runtime scanning.");
            }
        }

        private void DrawMigratorTab()
        {
            GUILayout.Label("VContainer to PirexZenith Migrator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "A powerful tool to automatically convert VContainer syntax in your scripts, scenes, and prefabs to PirexZenith.", 
                MessageType.Info);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("🛠️ Open Migrator Tool", GUILayout.Height(40)))
            {
                VContainerToPirexZenithMigrator.ShowWindow();
            }
        }
    }
}
