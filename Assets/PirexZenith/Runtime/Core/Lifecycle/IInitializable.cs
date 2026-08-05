namespace PirexZenith
{
    /// <summary>
    /// Interface for Pure C# Systems requiring initialization immediately after
    /// the LifetimeScope finishes building the DIContainer.
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Called once when the LifetimeScope successfully initializes the container.
        /// </summary>
        void Initialize();
    }
}
