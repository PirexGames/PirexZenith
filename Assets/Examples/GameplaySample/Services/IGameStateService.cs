namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Service interface for managing game state and scoring.
    /// </summary>
    public interface IGameStateService
    {
        int CurrentScore { get; }
        bool IsGameOver { get; }
        void AddScore(int amount);
        void SetGameOver();
        void ResetGame();
    }
}
