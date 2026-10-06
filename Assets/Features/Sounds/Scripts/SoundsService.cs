using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Features.Sounds
{
    public sealed class SoundsService : MonoBehaviour, ISoundsService, IInitializable
    {
        public float SfxVolume => _settings.SfxVolume;
        public float MusicVolume => _settings.MusicVolume;
        public bool SfxMuted => _settings.SfxMuted;
        public bool MusicMuted => _settings.MusicMuted;
        public SoundId CurrentMusic { get; private set; } = SoundId.None;

        private readonly Dictionary<SoundId, SoundDefinition> _sounds = new();
        private readonly HashSet<SoundId> _reportedInvalidSounds = new();

        private SoundsCatalog _catalog;
        private ISoundSettingsStorage _settingsStorage;
        private SoundSettingsData _settings;
        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        private float _currentMusicVolume = 1f;
        private bool _initialized;
        private float _nextUiHoverTime;
        private float _nextUiAdjustTime;

        [Inject]
        private void Construct(SoundsCatalog catalog, ISoundSettingsStorage settingsStorage)
        {
            _catalog = catalog;
            _settingsStorage = settingsStorage;
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            _settings = _settingsStorage.Load();
            BuildSoundsLookup();
            CreateAudioSources();
            ApplySettings();
            _initialized = true;
        }

        // Called after the UI catalog and its clips finish loading during bootstrap.
        public void SetUiSounds(SoundsCatalog catalog)
        {
            EnsureInitialized();

            if (catalog == null)
                throw new System.ArgumentNullException(nameof(catalog));

            foreach (SoundDefinition sound in catalog.Sounds)
            {
                if (sound == null || sound.Id == SoundId.None)
                    continue;

                _sounds[sound.Id] = sound;
                _reportedInvalidSounds.Remove(sound.Id);
            }
        }

        public void Play(SoundId soundId)
        {
            EnsureInitialized();

            if (!AllowUiFeedback(soundId))
                return;

            if (!TryGetSound(soundId, out SoundDefinition sound))
                return;

            switch (sound.Channel)
            {
                case SoundChannel.Sfx:
                    PlaySfx(sound);
                    break;
                case SoundChannel.Music:
                    PlayMusic(sound);
                    break;
                default:
                    ReportInvalidSound(soundId, $"Sound '{soundId}' has an unsupported channel.");
                    break;
            }
        }

        public void StopAllSfx()
        {
            EnsureInitialized();
            _sfxSource.Stop();
        }

        public void StopMusic()
        {
            EnsureInitialized();
            _musicSource.Stop();
            _musicSource.clip = null;
            CurrentMusic = SoundId.None;
            _currentMusicVolume = 1f;
        }

        public void SetSfxVolume(float volume)
        {
            EnsureInitialized();
            volume = Mathf.Clamp01(volume);

            if (Mathf.Approximately(_settings.SfxVolume, volume))
                return;

            _settings.SfxVolume = volume;
            _sfxSource.volume = volume;
            SaveSettings();
        }

        public void SetMusicVolume(float volume)
        {
            EnsureInitialized();
            volume = Mathf.Clamp01(volume);

            if (Mathf.Approximately(_settings.MusicVolume, volume))
                return;

            _settings.MusicVolume = volume;
            _musicSource.volume = volume * _currentMusicVolume;
            SaveSettings();
        }

        public void SetSfxMuted(bool isMuted)
        {
            EnsureInitialized();

            if (_settings.SfxMuted == isMuted)
                return;

            _settings.SfxMuted = isMuted;
            _sfxSource.mute = isMuted;
            SaveSettings();
        }

        public void SetMusicMuted(bool isMuted)
        {
            EnsureInitialized();

            if (_settings.MusicMuted == isMuted)
                return;

            _settings.MusicMuted = isMuted;
            _musicSource.mute = isMuted;
            SaveSettings();
        }

        private bool AllowUiFeedback(SoundId soundId)
        {
            float now = Time.unscaledTime;
            if (soundId == SoundId.UiHover)
            {
                if (now < _nextUiHoverTime)
                    return false;
                _nextUiHoverTime = now + 0.06f;
            }
            else if (soundId == SoundId.UiAdjust)
            {
                if (now < _nextUiAdjustTime)
                    return false;
                _nextUiAdjustTime = now + 0.08f;
            }
            else if (soundId == SoundId.UiClick || soundId == SoundId.UiSelect ||
                     soundId == SoundId.UiStartClick || soundId == SoundId.UiBack ||
                     soundId == SoundId.UiOpen || soundId == SoundId.UiClose ||
                     soundId == SoundId.UiError || soundId == SoundId.UiConfirm)
            {
                // Suppress focus changes caused by the same click or by opening a window.
                _nextUiHoverTime = now + 0.1f;
            }

            return true;
        }

        private void BuildSoundsLookup()
        {
            _sounds.Clear();

            foreach (SoundDefinition sound in _catalog.Sounds)
            {
                if (sound == null || sound.Id == SoundId.None)
                    continue;

                if (_sounds.ContainsKey(sound.Id))
                {
                    Debug.LogError($"Sound '{sound.Id}' is registered more than once.", _catalog);
                    continue;
                }

                _sounds.Add(sound.Id, sound);
            }
        }

        private void CreateAudioSources()
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;
        }

        private void ApplySettings()
        {
            _sfxSource.volume = _settings.SfxVolume;
            _sfxSource.mute = _settings.SfxMuted;
            _musicSource.volume = _settings.MusicVolume * _currentMusicVolume;
            _musicSource.mute = _settings.MusicMuted;
        }

        private bool TryGetSound(SoundId soundId, out SoundDefinition sound)
        {
            if (soundId != SoundId.None && _sounds.TryGetValue(soundId, out sound))
            {
                if (sound.Clip != null)
                    return true;

                ReportInvalidSound(soundId, $"Sound '{soundId}' has no AudioClip assigned.");
                return false;
            }

            sound = null;

            if (soundId != SoundId.None)
                ReportInvalidSound(soundId, $"Sound '{soundId}' is missing from '{_catalog.name}'.");

            return false;
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            EnsureInitialized();

            if (clip == null || _settings.SfxMuted || _settings.SfxVolume <= 0f)
                return;

            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void PlaySfx(SoundDefinition sound) =>
            PlaySfx(sound.Clip, sound.Volume);

        private void PlayMusic(SoundDefinition sound)
        {
            if (CurrentMusic == sound.Id && _musicSource.isPlaying)
                return;

            _musicSource.Stop();
            _musicSource.clip = sound.Clip;
            _musicSource.loop = sound.Loop;
            _currentMusicVolume = sound.Volume;
            _musicSource.volume = _settings.MusicVolume * _currentMusicVolume;
            CurrentMusic = sound.Id;
            _musicSource.Play();
        }

        private void SaveSettings() =>
            _settingsStorage.Save(_settings);

        private void ReportInvalidSound(SoundId soundId, string message)
        {
            if (_reportedInvalidSounds.Add(soundId))
                Debug.LogWarning(message, this);
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                Initialize();
        }
    }
}
