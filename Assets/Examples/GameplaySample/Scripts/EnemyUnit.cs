using PirexZenith;using UnityEngine;

namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Component attached to the Enemy Prefab.
    /// Demonstrates automatic dependency injection and O(1) object pooling lifecycle with RexPool.
    /// </summary>
    public class EnemyUnit : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float _lifeTime = 4f;

        [Inject] private IAudioService _audioService;
        [Inject] private IGameStateService _gameStateService;

        private float _timer;

        private void Start()
        {
            Debug.Log($"[PirexZenith Example - EnemyUnit ({gameObject.name})] Start() - Dependencies injected successfully!");
        }

        private void OnEnable()
        {
            _timer = 0f;
            _audioService?.PlaySFX("SFX_EnemySpawn");
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= _lifeTime)
            {
                _timer = 0f;
                Despawn();
            }
        }

        private void OnDisable()
        {
            _audioService?.PlaySFX("SFX_EnemyExplode");
            _gameStateService?.AddScore(5);
        }

        /// <summary>
        /// Despawns this enemy. Automatically returns to RexPool if installed, otherwise destroys the GameObject.
        /// </summary>
        public void Despawn()
        {
#if PIREX_REXPOOL_ENABLED
            Debug.Log($"[EnemyUnit ({gameObject.name})] Despawned -> Returned to RexPool O(1) free pool!");
            gameObject.SetActive(false); // RexPoolObject automatically pushes back to free stack on disable
#else
            Debug.Log($"[EnemyUnit ({gameObject.name})] Despawned -> Destroyed GameObject!");
            Destroy(gameObject);
#endif
        }
    }
}
