using UnityEngine;

namespace Features.Sounds
{
    // The AudioSource is configured on the prefab, loaded with the gameplay catalog.
    [DisallowMultipleComponent]
    public sealed class SoundLoopEmitter : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;

        public SoundId Sound { get; private set; }
        private ISoundsService _sounds;
        private float _volume;
        private bool _paused;
        private bool _initialized;

        public void Initialize(ISoundsService sounds, SoundDefinition definition, AudioClip clip)
        {
            _sounds = sounds;
            Sound = definition.Id;
            _volume = definition.Volume;
            _source.clip = clip;
            _initialized = true;
            ApplySettings();
            if (Time.timeScale > 0f)
                _source.Play();
            else
                _paused = true;
        }

        private void Update()
        {
            if (!_initialized)
                return;

            ApplySettings();
            bool paused = Time.timeScale <= 0f;
            if (paused == _paused)
                return;
            _paused = paused;
            if (paused)
                _source.Pause();
            else if (_source.timeSamples > 0)
                _source.UnPause();
            else
                _source.Play();
        }

        private void ApplySettings()
        {
            _source.volume = _volume * _sounds.SfxVolume;
            _source.mute = _sounds.SfxMuted;
        }

        public void Stop()
        {
            _initialized = false;
            _source.Stop();
        }

        private void OnDisable() => Stop();
    }
}
