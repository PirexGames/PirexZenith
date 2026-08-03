namespace PirexZenith.Core.Lifecycle
{
    /// <summary>
    /// Interface for Pure C# Systems requiring per-frame update execution
    /// within the LifetimeScope's Update loop without MonoBehaviour dependency.
    /// </summary>
    public interface ITickable
    {
        /// <summary>
        /// Called every frame during the LifetimeScope's Update loop.
        /// </summary>
        void Tick();
    }
}
