#if UNITY_EDITOR
using System.Collections.Generic;
using PirexZenith;using PirexZenith.Editor;
using PirexZenith;using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PirexZenith.Examples.GameplaySample.Editor
{
    /// <summary>
    /// One-Click Generator tool that automatically generates the sample Prefab and Scene
    /// for GameplaySample, ready for PlayMode evaluation.
    /// </summary>
    public static class GameplaySampleSceneGenerator
    {
        [MenuItem("Tools/Examples/ðŸš€ Generate Gameplay Sample Scene & Prefab", false, 100)]
        public static void GenerateGameplaySample()
        {
            // 1. Validate and create required directory structures
            EnsureFolderExists("Assets/Examples/GameplaySample", "Prefabs");
            EnsureFolderExists("Assets/Examples/GameplaySample", "Scenes");

            // 2. Create Enemy Prefab preconfigured with DIPrefabCache
            string prefabPath = "Assets/Examples/GameplaySample/Prefabs/EnemyPrefab.prefab";
            GameObject enemyGO = new GameObject("Enemy_Unit");
            
            // Create enemy visual representation
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(enemyGO.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            // Attach EnemyUnit and DIPrefabCache components
            EnemyUnit enemyUnit = enemyGO.AddComponent<EnemyUnit>();
            DIPrefabCache cache = enemyGO.AddComponent<DIPrefabCache>();
            cache.SetCachedTargets(new List<MonoBehaviour> { enemyUnit });

            // Save as Prefab asset
            GameObject enemyPrefab = PrefabUtility.SaveAsPrefabAsset(enemyGO, prefabPath);
            Object.DestroyImmediate(enemyGO);

            // 3. Create a complete new Scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            newScene.name = "GameplaySampleScene";

            // Create Scope & Installer GameObject
            GameObject scopeGO = new GameObject("--- PIREX_DI_SCOPE ---");
            LifetimeScope scope = scopeGO.AddComponent<LifetimeScope>();
            GameplaySceneInstaller installer = scopeGO.AddComponent<GameplaySceneInstaller>();

            // Create Player GameObject
            GameObject playerGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerGO.name = "Player_SpaceFighter";
            playerGO.transform.position = Vector3.zero;
            playerGO.AddComponent<PlayerController>();

            // Create EnemySpawner GameObject
            GameObject spawnerGO = new GameObject("EnemySpawner");
            spawnerGO.transform.position = new Vector3(0f, 0f, 5f);
            EnemySpawner spawner = spawnerGO.AddComponent<EnemySpawner>();

            // Assign EnemyPrefab to the _enemyPrefab field of EnemySpawner via SerializedObject
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            SerializedProperty prefabProp = serializedSpawner.FindProperty("_enemyPrefab");
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = enemyPrefab;
                serializedSpawner.ApplyModifiedProperties();
            }

            // 4. Auto-Bake the entire Scene
            int bakeCount = DIEditorScanner.BakeScene(newScene);

            // 5. Save Scene
            string scenePath = "Assets/Examples/GameplaySample/Scenes/GameplaySampleScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);

            // 6. Highlight object and display success confirmation
            Selection.activeGameObject = scopeGO;
            Object sceneAsset = AssetDatabase.LoadAssetAtPath<Object>(scenePath);
            EditorGUIUtility.PingObject(sceneAsset);

            EditorUtility.DisplayDialog(
                "PirexZenith - One-Click Example Generator",
                $"Successfully generated Gameplay Sample scene!\n\n" +
                $"ðŸ“¦ Prefab: {prefabPath}\n" +
                $"ðŸŽ¬ Scene: {scenePath}\n\n" +
                $"âš¡ Auto-Bake pre-populated {bakeCount} target(s) into LifetimeScope.\n" +
                $"You can press PLAY immediately to test!",
                "Awesome!");
        }

        private static void EnsureFolderExists(string parentFolder, string newFolderName)
        {
            string fullPath = parentFolder + "/" + newFolderName;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
            }
        }
    }
}
#endif
