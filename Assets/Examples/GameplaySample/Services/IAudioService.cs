namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Game audio service interface.
    /// Demonstrates interface-based binding (RegisterSingleton&lt;AudioService, IAudioService&gt;).
    /// </summary>
    public interface IAudioService
    {
        void PlaySFX(string soundName);
        void PlayMusic(string musicName);
        void SetMute(bool isMuted);
    }
}
