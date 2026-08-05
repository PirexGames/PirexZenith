namespace PirexZenith
{
    /// <summary>
    /// Specifies the lifetime of a service registered in a DIContainer.
    /// </summary>
    public enum Lifetime
    {
        /// <summary>
        /// A new instance is created every time the service is resolved.
        /// </summary>
        Transient,

        /// <summary>
        /// A single shared instance is created for the entire lifetime of the container.
        /// </summary>
        Singleton,

        /// <summary>
        /// Uses a pre-existing instance provided at registration time.
        /// </summary>
        Instance
    }
}
