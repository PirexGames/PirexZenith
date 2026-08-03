using System;
using UnityEngine;
using PirexZenith.Core;
using PirexZenith.Core.Lifecycle;

namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Example of a Pure C# System that does NOT inherit from MonoBehaviour.
    /// This system is automatically initialized via IInitializable, updated per-frame in Update() via ITickable,
    /// and cleaned up via IDisposable when the LifetimeScope is destroyed.
    /// </summary>
    public class GameplayScoreSystem : IInitializable, ITickable, IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly IGameStateService _gameState;
        private float _timeSinceLastBonus = 0f;

        // Dependency injection via constructor (recommended pattern for Pure C# Systems)
        [Inject]
        public GameplayScoreSystem(IAudioService audioService, IGameStateService gameState)
        {
            _audioService = audioService;
            _gameState = gameState;
        }

        public void Initialize()
        {
            Debug.Log("[PirexZenith Example - GameplayScoreSystem] Initialize() - Pure C# System initialized successfully without MonoBehaviour!");
            _gameState.AddScore(50); // Welcome bonus points upon system initialization
            _audioService.PlaySFX("SFX_SystemBoot");
        }

        public void Tick()
        {
            // Executed every frame within the LifetimeScope's Update() loop
            _timeSinceLastBonus += Time.deltaTime;

            // Grant survival points automatically every 30 seconds of gameplay
            if (_timeSinceLastBonus >= 30f)
            {
                _timeSinceLastBonus = 0f;
                _gameState.AddScore(15);
                Debug.Log($"[GameplayScoreSystem] +15 Survival Points! Total score: {_gameState.CurrentScore}");
            }
        }

        public void Dispose()
        {
            Debug.Log("[GameplayScoreSystem] Dispose() - Pure C# System disposed automatically upon scene unload or exit.");
        }
    }
}
