using PirexZenith.Core;
using UnityEngine;

namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Sample player controller component.
    /// Demonstrates Field Injection, Property Injection, and Transient object resolution.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // 1. Field Injection (injecting into private fields)
        [Inject] private IAudioService _audioService;

        // 2. Property Injection (injecting into properties)
        [Inject] public IGameStateService GameState { get; private set; }

        // 3. Transient Injection (receiving a new distinct PlayerStats instance)
        [Inject] private PlayerStats _stats;

        private void Start()
        {
            Debug.Log("[PirexZenith Example - PlayerController] Start() - Verifying injected services:");

            if (_audioService != null)
            {
                _audioService.PlayMusic("BGM_SpaceBattle");
            }
            else
            {
                Debug.LogError("[PlayerController] _audioService is NULL! Ensure LifetimeScope is configured properly.");
            }

            if (_stats != null)
            {
                Debug.Log($"[PlayerController] Initial Player Stats - Health: {_stats.Health}, Speed: {_stats.MoveSpeed}");
            }
        }

        private void Update()
        {
            // Test adding score when pressing Space (Supports both New Input System and Legacy Input)
            if (UniversalInput.GetKeyDown(KeyCode.Space, "spaceKey"))
            {
                _audioService?.PlaySFX("SFX_LaserShoot");
                GameState?.AddScore(10);
            }

            // Test taking damage when pressing D (Supports both New Input System and Legacy Input)
            if (UniversalInput.GetKeyDown(KeyCode.D, "dKey"))
            {
                _stats?.TakeDamage(15);
                _audioService?.PlaySFX("SFX_PlayerHit");
            }
        }
    }
}
