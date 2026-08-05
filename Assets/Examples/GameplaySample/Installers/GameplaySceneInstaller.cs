using PirexZenith;using PirexZenith;using UnityEngine;

namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Sample installer for the Gameplay Scene.
    /// Attach this script to a GameObject in the Scene alongside a <see cref="LifetimeScope"/> (or assign it to the LifetimeScope's Installers list).
    /// </summary>
    public class GameplaySceneInstaller : MonoInstaller
    {
        public override void InstallBindings(ContainerBuilder builder)
        {
            Debug.Log("[PirexZenith Example] GameplaySceneInstaller is registering bindings into the container...");

            // 1. Singleton Registration: Single shared instance across the container's lifecycle
            builder.RegisterSingleton<AudioService, IAudioService>();
            builder.RegisterSingleton<GameStateService, IGameStateService>();

            // 2. Transient Registration: A new instance is created on each dependency resolution request
            builder.RegisterTransient<PlayerStats>();

            // 3. Pure C# System Registration: Independent of MonoBehaviour, automatically managed for IInitializable, ITickable, IDisposable
            builder.RegisterSystem<GameplayScoreSystem>();

            Debug.Log("[PirexZenith Example] Completed registering bindings!");
        }
    }
}
