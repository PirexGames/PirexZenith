using UnityEngine;

namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Implementation of <see cref="IGameStateService"/>, registered as a Singleton.
    /// </summary>
    public class GameStateService : IGameStateService
    {
        public int CurrentScore { get; private set; }
        public bool IsGameOver { get; private set; }

        public GameStateService()
        {
            Debug.Log("[PirexZenith Example] GameStateService initialized (Singleton Instance Created).");
            ResetGame();
        }

        public void AddScore(int amount)
        {
            if (IsGameOver) return;
            CurrentScore += amount;
            Debug.Log($"[PirexZenith Example - GameStateService] Current score: {CurrentScore} (+{amount})");
        }

        public void SetGameOver()
        {
            IsGameOver = true;
            Debug.LogWarning("[PirexZenith Example - GameStateService] Game Over!");
        }

        public void ResetGame()
        {
            CurrentScore = 0;
            IsGameOver = false;
            Debug.Log("[PirexZenith Example - GameStateService] Game state reset (ResetGame).");
        }
    }
}
