using UnityEngine;

namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Implementation of the audio service <see cref="IAudioService"/>.
    /// Registered as a Singleton within the Container.
    /// </summary>
    public class AudioService : IAudioService
    {
        private bool _isMuted;

        public AudioService()
        {
            Debug.Log("[PirexZenith Example] AudioService initialized (Singleton Instance Created).");
        }

        public void PlaySFX(string soundName)
        {
            if (_isMuted) return;
            Debug.Log($"[PirexZenith Example - AudioService] PlaySFX: {soundName}");
        }

        public void PlayMusic(string musicName)
        {
            if (_isMuted) return;
            Debug.Log($"[PirexZenith Example - AudioService] PlayMusic: {musicName}");
        }

        public void SetMute(bool isMuted)
        {
            _isMuted = isMuted;
            Debug.Log($"[PirexZenith Example - AudioService] Muted: {_isMuted}");
        }
    }
}
