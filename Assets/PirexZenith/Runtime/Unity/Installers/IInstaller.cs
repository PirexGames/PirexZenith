namespace PirexZenith
{
    /// <summary>
    /// Core interface for service installers.
    /// </summary>
    public interface IInstaller
    {
        /// <summary>
        /// Registers service bindings into the provided ContainerBuilder.
        /// </summary>
        void InstallBindings(ContainerBuilder builder);
    }
}
