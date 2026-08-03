namespace PirexZenith.Core.Lifecycle
{
    /// <summary>
    /// Interface for Pure C# Systems requiring execution at the end of each frame
    /// within the LifetimeScope's LateUpdate loop.
    /// </summary>
    public interface ILateTickable
    {
        /// <summary>
        /// Called during the LifetimeScope's LateUpdate loop.
        /// </summary>
        void LateTick();
    }
}
