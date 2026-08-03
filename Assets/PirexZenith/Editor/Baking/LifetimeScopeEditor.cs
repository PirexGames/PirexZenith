using PirexZenith.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PirexZenith.Editor
{
    /// <summary>
    /// Custom Editor for <see cref="LifetimeScope"/> (and derived classes such as <see cref="SceneScope"/>, <see cref="ProjectScope"/>).
    /// Displays Bake status, Scope type (Project / Scene / Sub-object), and runtime Parent Scope resolution information in the Inspector.
    /// </summary>
    [CustomEditor(typeof(LifetimeScope), true)]
    public class LifetimeScopeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            LifetimeScope scope = (LifetimeScope)target;

            serializedObject.Update();

            // 1. Render default inspector properties (skip _parentScopeName to draw Custom Popup)
            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.name == "_parentScopeName") continue;
                    EditorGUILayout.PropertyField(prop, true);
                }
                while (prop.NextVisible(false));
            }

            EditorGUILayout.Space(5);

            // 2. Render Custom Dropdown displaying all classes derived from LifetimeScope in the project
            DrawParentScopeClassPopup(scope);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("👑 PirexZenith LifetimeScope Status", EditorStyles.boldLabel);

            if (string.Equals(scope.ParentScopeName, "None", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.HelpBox("🔗 Standalone Scope (None): This scope operates independently without inheriting from any parent container.", MessageType.None);
            }
            else if (!string.IsNullOrEmpty(scope.ParentScopeName))
            {
                EditorGUILayout.HelpBox($"🔗 Named Parent Lookup: During initialization, this scope will resolve an active parent scope matching class name (nameof) or scene '{scope.ParentScopeName}'.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("🔗 Auto Parent Lookup (Default): During initialization, this scope will automatically link to the most recent active parent scope (e.g. SceneScope / ProjectScope).", MessageType.None);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("⚡ Auto-Baking & Performance Control", EditorStyles.boldLabel);

            int bakedCount = scope.bakedTargets != null ? scope.bakedTargets.Count : 0;
            if (bakedCount > 0)
            {
                EditorGUILayout.HelpBox($"Baked {bakedCount} target(s) into LifetimeScope. Runtime resolution will execute at O(1) performance!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Baked Targets list is empty! System will automatically switch to Bulletproof Fallback (runtime scanning) to ensure stability.", MessageType.Warning);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("⚡ Bake Targets (Scene/Prefab)", GUILayout.Height(28)))
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int count = 0;

                bool isPrefab = PrefabUtility.IsPartOfPrefabAsset(scope.gameObject);
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                bool inPrefabStage = prefabStage != null && prefabStage.IsPartOfPrefabContents(scope.gameObject);

                if (isPrefab || inPrefabStage)
                {
                    GameObject root = inPrefabStage ? prefabStage.prefabContentsRoot : scope.gameObject.transform.root.gameObject;
                    PrefabBakingSystem.BakePrefab(root, out count, out _);
                    stopwatch.Stop();
                    Debug.Log($"[PirexZenith] Manual Bake completed for Prefab '{root.name}': Baked {count} target(s) into LifetimeScope in {stopwatch.ElapsedMilliseconds} ms.");
                }
                else
                {
                    Scene scene = SceneManager.GetActiveScene();
                    count = DIEditorScanner.BakeScene(scene);
                    stopwatch.Stop();
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"[PirexZenith] Manual Bake completed for scene '{scene.name}': Baked {count} target(s) into LifetimeScope in {stopwatch.ElapsedMilliseconds} ms.");
                }
            }

            if (GUILayout.Button("🗑 Clear Baked Targets", GUILayout.Height(28)))
            {
                if (scope.bakedTargets != null)
                {
                    scope.bakedTargets.Clear();
                    EditorUtility.SetDirty(scope);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    Debug.Log("[PirexZenith] Cleared Baked Targets from LifetimeScope. The application will test the runtime Bulletproof Fallback mechanism.");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawParentScopeClassPopup(LifetimeScope scope)
        {
            var derivedTypes = TypeCache.GetTypesDerivedFrom<LifetimeScope>();
            var optionsList = new System.Collections.Generic.List<string>
            {
                "[ Auto / Inherit Active Parent Scope ]",
                "[ None / Standalone Scope ]"
            };

            foreach (var type in derivedTypes)
            {
                // Only include concrete classes derived from LifetimeScope (excluding base LifetimeScope and abstract classes)
                if (type != null && !type.IsAbstract && type != typeof(LifetimeScope))
                {
                    if (!optionsList.Contains(type.Name))
                    {
                        optionsList.Add(type.Name);
                    }
                }
            }

            string[] options = optionsList.ToArray();

            int currentIndex = 0;
            if (string.Equals(scope.ParentScopeName, "None", System.StringComparison.OrdinalIgnoreCase))
            {
                currentIndex = 1;
            }
            else if (!string.IsNullOrEmpty(scope.ParentScopeName))
            {
                for (int i = 2; i < options.Length; i++)
                {
                    if (string.Equals(options[i], scope.ParentScopeName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(new GUIContent("Parent Scope Class", "Select how this scope links to a parent scope. Auto (default) inherits from the active Scene/Project scope. None operates as a standalone scope."), currentIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(scope, "Change Parent Scope Class");
                if (newIndex == 0) scope.ParentScopeName = "";
                else if (newIndex == 1) scope.ParentScopeName = "None";
                else scope.ParentScopeName = options[newIndex];
                EditorUtility.SetDirty(scope);
            }
        }
    }
}
