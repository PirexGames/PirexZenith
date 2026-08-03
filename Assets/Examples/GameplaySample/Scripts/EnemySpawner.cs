using PirexZenith.Core;
using PirexZenith.Unity;
using UnityEngine;

namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Sample enemy spawning component.
    /// Demonstrates injecting <see cref="DIContainer"/> and calling pooling extension methods
    /// to instantiate or pool Prefabs with automatic dependency injection.
    /// Works seamlessly whether RexPool is installed (0-GC pooling) or not (standard Instantiate).
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private float _spawnInterval = 3f;

        // Directly inject the active Scene Container
        [Inject] private DIContainer _container;

        private float _timer;

        private void Start()
        {
            // Pre-warm the pool with 5 inactive enemy instances (0 runtime allocation spikes later!)
            // When RexPool is installed, this pre-warms the RexPoolManager pool. Otherwise, it is a safe no-op.
            if (_enemyPrefab != null)
            {
                _container.Prepair(_enemyPrefab, 5);
                Debug.Log("[PirexZenith Example - EnemySpawner] Pre-warmed pool with 5 enemy instances!");
            }
            Debug.Log("[PirexZenith Example - EnemySpawner] Ready to spawn enemies (press E key to spawn manually, C key to clean up pool).");
        }

        private void Update()
        {
            // Manual spawn when pressing E key (Supports both New Input System and Legacy Input)
            if (UniversalInput.GetKeyDown(KeyCode.E, "eKey"))
            {
                SpawnEnemy();
            }

            // Manual pool cleanup when pressing C key (Supports both New Input System and Legacy Input)
            if (UniversalInput.GetKeyDown(KeyCode.C, "cKey"))
            {
                _container.CleanUp(_enemyPrefab);
                Debug.Log("[PirexZenith Example - EnemySpawner] Cleaned up all pooled enemy instances!");
            }

            // Or spawn periodically (if a prefab is assigned)
            if (_enemyPrefab != null)
            {
                _timer += Time.deltaTime;
                if (_timer >= _spawnInterval)
                {
                    _timer = 0f;
                    SpawnEnemy();
                }
            }
        }

        /// <summary>
        /// Spawns an Enemy Prefab through the Container to automatically inject dependencies into EnemyUnit.
        /// Uses O(1) RexPool object pooling if installed, or falls back seamlessly to standard instantiation.
        /// </summary>
        public GameObject SpawnEnemy()
        {
            if (_enemyPrefab == null)
            {
                Debug.LogWarning("[EnemySpawner] EnemyPrefab is unassigned! Assign a Prefab in the Inspector or press Play to evaluate other features.");
                return null;
            }

            if (_container == null)
            {
                Debug.LogError("[EnemySpawner] DIContainer is NULL! Ensure a LifetimeScope is active in the Scene.");
                return null;
            }

            Vector3 spawnPos = transform.position + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));

            // Call the Take extension method on DIContainer (automatically uses RexPool if installed, or falls back to standard Instantiate)
            GameObject enemyInstance = _container.Take(_enemyPrefab, spawnPos, Quaternion.identity, null);
            enemyInstance.name = $"Enemy_{Random.Range(100, 999)}";

            Debug.Log($"[EnemySpawner] Spawned {enemyInstance.name} at {spawnPos} with O(1) DI & Pooling!");
            return enemyInstance;
        }
    }
}
