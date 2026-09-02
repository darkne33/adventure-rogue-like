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

        public void Play(SoundId soundId)
        {
            EnsureInitialized();

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

        private void PlaySfx(SoundDefinition sound)
        {
            if (_settings.SfxMuted || _settings.SfxVolume <= 0f)
                return;

            _sfxSource.PlayOneShot(sound.Clip, sound.Volume);
        }

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
