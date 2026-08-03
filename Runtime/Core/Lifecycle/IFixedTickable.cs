namespace PirexZenith.Core.Lifecycle
{
    /// <summary>
    /// Interface for Pure C# Systems requiring physics update execution
    /// within the LifetimeScope's FixedUpdate loop.
    /// </summary>
    public interface IFixedTickable
    {
        /// <summary>
        /// Called during the LifetimeScope's FixedUpdate physics loop.
        /// </summary>
        void FixedTick();
    }
}
