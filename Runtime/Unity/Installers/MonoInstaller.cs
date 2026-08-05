using UnityEngine;

namespace PirexZenith
{
    /// <summary>
    /// Base class for MonoBehaviour installers attached to GameObjects in a Unity Scene.
    /// </summary>
    public abstract class MonoInstaller : MonoBehaviour, IInstaller
    {
        /// <summary>
        /// Override this method to configure service bindings in the ContainerBuilder.
        /// </summary>
        public abstract void InstallBindings(ContainerBuilder builder);
    }
}
