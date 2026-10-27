using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Features.Sounds
{
    public sealed class SoundsService : ISoundsService, IInitializable, ITickable, ILateTickable, System.IDisposable
    {
        public float SfxVolume => _settings.SfxVolume;
        public float MusicVolume => _settings.MusicVolume;
        public bool SfxMuted => _settings.SfxMuted;
        public bool MusicMuted => _settings.MusicMuted;
        public SoundId CurrentMusic { get; private set; } = SoundId.None;

        private readonly Dictionary<SoundId, SoundDefinition> _sounds = new();
        private readonly HashSet<SoundId> _reportedInvalidSounds = new();
        private readonly Dictionary<SoundId, float> _nextPlayTimes = new();
        private readonly Dictionary<SoundId, int> _lastVariants = new();
        private readonly List<SoundLoopEmitter> _loops = new();
        private SoundLoopEmitter _loopPrefab;

        private SoundsCatalog _catalog;
        private ISoundSettingsStorage _settingsStorage;
        private SoundSettingsData _settings;
        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        private SoundPlayback _playback;
        private SoundDefinition _pendingMusic;
        private float _currentMusicVolume = 1f;
        private float _musicGain = 1f;
        private float _pauseFilterBlend;
        private bool _gameplayPaused;
        private bool _disposed;
        private bool _initialized;
        private float _nextUiHoverTime;
        private float _nextUiAdjustTime;
        private SoundId _pendingUiFeedback;
        private int _lastUiActionFrame = -1;

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
            ApplySettings();
            _initialized = true;
        }

        // The catalog and all prefab/clip dependencies have finished Addressables loading.
        public void SetMusicSounds(SoundsCatalog catalog)
        {
            EnsureInitialized();
            if (catalog == null || catalog.PlaybackPrefab == null)
                throw new System.InvalidOperationException("Music catalog requires a prepared SoundPlayback prefab.");
            if (_disposed)
                throw new System.ObjectDisposedException(nameof(SoundsService));

            if (_playback == null)
            {
                _playback = UnityEngine.Object.Instantiate(catalog.PlaybackPrefab);
                UnityEngine.Object.DontDestroyOnLoad(_playback.gameObject);
                _sfxSource = _playback.GameSounds;
                _musicSource = _playback.Music;
                ApplySettings();
            }

            MergeSounds(catalog);
        }

        public void Tick()
        {
            if (_playback == null || _disposed)
                return;

            UpdateGameplayPause();
            UpdateMusicFade();
            UpdatePauseFilter();
        }

        public void LateTick()
        {
            SoundId pending = _pendingUiFeedback;
            _pendingUiFeedback = SoundId.None;
            if (pending != SoundId.None && !_disposed && _playback != null)
                PlayImmediate(pending);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopAllSfx();
            StopMusic();
            if (_playback != null)
            {
                UnityEngine.Object.Destroy(_playback.gameObject);
                _playback = null;
            }
            _sfxSource = null;
            _musicSource = null;
            _sounds.Clear();
            _loopPrefab = null;
        }

        // Called after the UI catalog and its clips finish loading during bootstrap.
        public void SetUiSounds(SoundsCatalog catalog)
        {
            MergeSounds(catalog);
        }

        public void SetGameplaySounds(SoundsCatalog catalog)
        {
            StopGameplayLoops();
            MergeSounds(catalog);
            _loopPrefab = catalog.LoopPrefab;
        }

        private void MergeSounds(SoundsCatalog catalog)
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
                _nextPlayTimes.Remove(sound.Id);
                _lastVariants.Remove(sound.Id);
            }
        }

        public void Play(SoundId soundId)
        {
            EnsureInitialized();

            if (_disposed)
                return;

            // Resolve incidental focus/value feedback after this frame's UI actions.
            if (soundId == SoundId.UiHover || soundId == SoundId.UiAdjust)
            {
                if (_lastUiActionFrame != Time.frameCount && _pendingUiFeedback != SoundId.UiAdjust)
                    _pendingUiFeedback = soundId;
                return;
            }

            PlayImmediate(soundId);
        }

        private void PlayImmediate(SoundId soundId)
        {
            if (!AllowUiFeedback(soundId))
                return;

            if (!TryGetSound(soundId, out SoundDefinition sound))
                return;

            RequirePlayback();
            if (sound.Channel != SoundChannel.Music && (sound.Loop || !AllowPlayback(sound)))
                return;

            switch (sound.Channel)
            {
                case SoundChannel.Sfx:
                case SoundChannel.Ui:
                case SoundChannel.XpAndGold:
                case SoundChannel.Ambience:
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

        public void StopGameplaySfx()
        {
            EnsureInitialized();
            if (_playback != null)
            {
                _sfxSource.Stop();
                _playback.XpAndGold.Stop();
                _playback.Ambience.Stop();
            }
            StopGameplayLoops();
        }

        public void StopAllSfx()
        {
            _pendingUiFeedback = SoundId.None;
            StopGameplaySfx();
            if (_playback != null)
                _playback.Ui.Stop();
        }

        public void PlayLoop(SoundId soundId, Transform owner)
        {
            EnsureInitialized();
            if (owner == null || _loopPrefab == null ||
                !TryGetSound(soundId, out SoundDefinition sound) || !sound.Loop ||
                sound.Channel != SoundChannel.Sfx)
                return;

            int instances = 0;
            for (int i = _loops.Count - 1; i >= 0; i--)
            {
                SoundLoopEmitter loop = _loops[i];
                if (loop == null)
                {
                    _loops.RemoveAt(i);
                    continue;
                }
                if (loop.Sound != soundId)
                    continue;
                if (loop.transform.parent == owner)
                    return;
                instances++;
            }
            if (instances >= sound.MaxLoopInstances)
                return;

            SoundLoopEmitter emitter = UnityEngine.Object.Instantiate(_loopPrefab, owner, false);
            emitter.Initialize(this, sound, GetClip(sound));
            _loops.Add(emitter);
        }

        public void StopGameplayLoops()
        {
            foreach (SoundLoopEmitter loop in _loops)
            {
                if (loop != null)
                {
                    loop.Stop();
                    UnityEngine.Object.Destroy(loop.gameObject);
                }
            }
            _loops.Clear();
            _nextPlayTimes.Clear();
        }

        private bool AllowPlayback(SoundDefinition sound)
        {
            if (_settings.SfxMuted || _settings.MasterVolume <= 0f ||
                GetVolume(GetCategory(sound.Channel)) <= 0f)
                return false;
            if (sound.Channel != SoundChannel.Ui && Time.timeScale <= 0f)
                return false;
            float now = Time.unscaledTime;
            if (_nextPlayTimes.TryGetValue(sound.Id, out float nextTime) && now < nextTime)
                return false;
            if (sound.MinimumInterval > 0f)
                _nextPlayTimes[sound.Id] = now + sound.MinimumInterval;
            return true;
        }

        private AudioClip GetClip(SoundDefinition sound)
        {
            AudioClip[] variants = sound.Variants;
            if (variants == null || variants.Length == 0)
                return sound.Clip;

            int count = variants.Length + 1;
            int index = Random.Range(0, count);
            if (_lastVariants.TryGetValue(sound.Id, out int previous) && index == previous)
                index = (index + Random.Range(1, count)) % count;
            _lastVariants[sound.Id] = index;
            return index == 0 || variants[index - 1] == null ? sound.Clip : variants[index - 1];
        }

        public void StopMusic()
        {
            EnsureInitialized();
            if (_musicSource != null)
            {
                _musicSource.Stop();
                _musicSource.clip = null;
                _playback.MusicLowPass.enabled = false;
                _playback.MusicLowPass.cutoffFrequency = 22000f;
            }
            _pendingMusic = null;
            CurrentMusic = SoundId.None;
            _currentMusicVolume = 1f;
            _musicGain = 1f;
            _pauseFilterBlend = 0f;
        }

        public void RestartMusic(SoundId soundId)
        {
            EnsureInitialized();
            if (!TryGetSound(soundId, out SoundDefinition sound) || sound.Channel != SoundChannel.Music)
                return;
            RequirePlayback();
            PlayMusic(sound, true);
        }

        public float GetVolume(SoundVolumeCategory category)
        {
            EnsureInitialized();
            return category switch
            {
                SoundVolumeCategory.Master => _settings.MasterVolume,
                SoundVolumeCategory.Music => _settings.MusicMuted ? 0f : _settings.MusicVolume,
                SoundVolumeCategory.GameSounds => _settings.SfxMuted ? 0f : _settings.SfxVolume,
                SoundVolumeCategory.Ambience => _settings.SfxMuted ? 0f : _settings.AmbienceVolume,
                SoundVolumeCategory.Ui => _settings.SfxMuted ? 0f : _settings.UiVolume,
                SoundVolumeCategory.XpAndGold => _settings.SfxMuted ? 0f : _settings.XpAndGoldVolume,
                _ => throw new System.ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }

        public void SetVolume(SoundVolumeCategory category, float volume)
        {
            EnsureInitialized();
            volume = Mathf.Clamp01(volume);
            if (Mathf.Approximately(GetVolume(category), volume))
                return;

            switch (category)
            {
                case SoundVolumeCategory.Master: _settings.MasterVolume = volume; break;
                case SoundVolumeCategory.Music: _settings.MusicVolume = volume; break;
                case SoundVolumeCategory.GameSounds: _settings.SfxVolume = volume; break;
                case SoundVolumeCategory.Ambience: _settings.AmbienceVolume = volume; break;
                case SoundVolumeCategory.Ui: _settings.UiVolume = volume; break;
                case SoundVolumeCategory.XpAndGold: _settings.XpAndGoldVolume = volume; break;
                default: throw new System.ArgumentOutOfRangeException(nameof(category), category, null);
            }
            // A nonzero slider also recovers legacy saved mute flags without a hidden mute state.
            if (volume > 0f)
            {
                if (category == SoundVolumeCategory.Music)
                    _settings.MusicMuted = false;
                else if (category != SoundVolumeCategory.Master)
                    _settings.SfxMuted = false;
            }
            ApplySettings();
            SaveSettings();
        }

        public void SetSfxVolume(float volume) => SetVolume(SoundVolumeCategory.GameSounds, volume);
        public void SetMusicVolume(float volume) => SetVolume(SoundVolumeCategory.Music, volume);

        public void SetSfxMuted(bool isMuted)
        {
            EnsureInitialized();

            if (_settings.SfxMuted == isMuted)
                return;

            _settings.SfxMuted = isMuted;
            ApplySettings();
            SaveSettings();
        }

        public void SetMusicMuted(bool isMuted)
        {
            EnsureInitialized();

            if (_settings.MusicMuted == isMuted)
                return;

            _settings.MusicMuted = isMuted;
            ApplySettings();
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
                     soundId == SoundId.UiError || soundId == SoundId.UiConfirm ||
                     soundId == SoundId.UiPurchase || soundId == SoundId.UiClaimReward ||
                     soundId == SoundId.UiUpgradeConfirm || soundId == SoundId.UiRelicTake ||
                     soundId == SoundId.UiRelicSkip || soundId == SoundId.UiUpgradeOpen ||
                     soundId == SoundId.UiUpgradeSkip)
            {
                // Suppress focus changes caused by the same click or by opening a window.
                _pendingUiFeedback = SoundId.None;
                _lastUiActionFrame = Time.frameCount;
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

        private void ApplySettings()
        {
            if (_playback == null)
                return;

            _sfxSource.volume = _settings.MasterVolume * _settings.SfxVolume;
            _sfxSource.mute = _settings.SfxMuted;
            _playback.Ui.volume = _settings.MasterVolume * _settings.UiVolume;
            _playback.Ui.mute = _settings.SfxMuted;
            _playback.XpAndGold.volume = _settings.MasterVolume * _settings.XpAndGoldVolume;
            _playback.XpAndGold.mute = _settings.SfxMuted;
            _playback.Ambience.volume = _settings.MasterVolume * _settings.AmbienceVolume;
            _playback.Ambience.mute = _settings.SfxMuted;
            ApplyMusicVolume();
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

            if (clip == null || _settings.SfxMuted || _settings.SfxVolume <= 0f ||
                _settings.MasterVolume <= 0f || Time.timeScale <= 0f)
                return;

            RequirePlayback();
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void PlaySfx(SoundDefinition sound)
        {
            AudioSource source = sound.Channel switch
            {
                SoundChannel.Sfx => _sfxSource,
                SoundChannel.Ui => _playback.Ui,
                SoundChannel.XpAndGold => _playback.XpAndGold,
                SoundChannel.Ambience => _playback.Ambience,
                _ => throw new System.ArgumentOutOfRangeException(nameof(sound.Channel))
            };
            source.PlayOneShot(GetClip(sound), sound.Volume);
        }

        private void PlayMusic(SoundDefinition sound, bool restart = false)
        {
            if (!restart && (_pendingMusic?.Id == sound.Id ||
                (_pendingMusic == null && CurrentMusic == sound.Id && _musicSource.isPlaying)))
                return;

            if (_musicSource.isPlaying && _playback.MusicFadeDuration > 0f)
            {
                _pendingMusic = sound;
                return;
            }

            StartMusic(sound);
        }

        private void StartMusic(SoundDefinition sound)
        {
            _pendingMusic = null;
            _musicSource.Stop();
            _musicSource.clip = sound.Clip;
            _musicSource.loop = sound.Loop;
            _currentMusicVolume = sound.Volume;
            _musicGain = _playback.MusicFadeDuration > 0f ? 0f : 1f;
            CurrentMusic = sound.Id;
            ApplyMusicVolume();
            _musicSource.Play();
        }

        private void UpdateMusicFade()
        {
            float duration = _playback.MusicFadeDuration;
            _musicGain = duration <= 0f ? (_pendingMusic == null ? 1f : 0f) :
                Mathf.MoveTowards(_musicGain, _pendingMusic == null ? 1f : 0f, Time.unscaledDeltaTime / duration);

            if (_pendingMusic != null && _musicGain <= 0f)
                StartMusic(_pendingMusic);

            ApplyMusicVolume();
        }

        private void ApplyMusicVolume() =>
            _musicSource.volume = _settings.MasterVolume * _settings.MusicVolume * _currentMusicVolume * _musicGain;

        private void UpdateGameplayPause()
        {
            bool paused = Time.timeScale <= 0f;
            if (_gameplayPaused == paused)
                return;
            _gameplayPaused = paused;

            SetPaused(_sfxSource, paused);
            SetPaused(_playback.XpAndGold, paused);
            SetPaused(_playback.Ambience, paused);
        }

        private static void SetPaused(AudioSource source, bool paused)
        {
            if (paused)
                source.Pause();
            else
                source.UnPause();
        }

        private void UpdatePauseFilter()
        {
            // Pause never changes music position or pitch, including upgrade/reward screens.
            bool filtered = (CurrentMusic == SoundId.GameplayMusic || CurrentMusic == SoundId.BossMusic) &&
                            Time.timeScale <= 0f;
            float duration = _playback.PauseFilterDuration;
            _pauseFilterBlend = duration <= 0f ? (filtered ? 1f : 0f) :
                Mathf.MoveTowards(_pauseFilterBlend, filtered ? 1f : 0f, Time.unscaledDeltaTime / duration);

            AudioLowPassFilter filter = _playback.MusicLowPass;
            filter.enabled = _pauseFilterBlend > 0f;
            filter.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(22000f),
                Mathf.Log(_playback.PausedMusicCutoff), _pauseFilterBlend));
        }

        private static SoundVolumeCategory GetCategory(SoundChannel channel) => channel switch
        {
            SoundChannel.Sfx => SoundVolumeCategory.GameSounds,
            SoundChannel.Ui => SoundVolumeCategory.Ui,
            SoundChannel.XpAndGold => SoundVolumeCategory.XpAndGold,
            SoundChannel.Ambience => SoundVolumeCategory.Ambience,
            SoundChannel.Music => SoundVolumeCategory.Music,
            _ => throw new System.ArgumentOutOfRangeException(nameof(channel), channel, null)
        };

        private void RequirePlayback()
        {
            if (_playback == null)
                throw new System.InvalidOperationException("Audio playback must be loaded during bootstrap before use.");
        }

        private void SaveSettings() =>
            _settingsStorage.Save(_settings);

        private void ReportInvalidSound(SoundId soundId, string message)
        {
            if (_reportedInvalidSounds.Add(soundId))
                Debug.LogWarning(message, _playback);
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                Initialize();
        }
    }
}
