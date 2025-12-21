using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Infrastructure.SaveSystem;
using UnityEngine;
using Zenject;

namespace Core.Sounds
{
    public class SoundsService : ISoundsService, ISaveReader<PlayerGlobalSaveData>, ISaveWriter<PlayerGlobalSaveData>
    {
        public bool SoundsIsMute => _saveData.SoundsIsMute;
        public bool MusicIsMute => _saveData.MusicIsMute;

        [Inject] private DiContainer _diContainer;
        [Inject] private SoundsConfig _soundsConfig;
        [Inject] private ISoundsStorage _soundsStorage;
        [Inject] private IPlayerSaveLoadService _playerSaveLoadService;

        private readonly List<AudioSource> _audioSources = new();

        private AudioSource _ambientAudioSource;

        private readonly Dictionary<AudioClipName, float> _musicsData = new();
        private SoundsSaveData _saveData;

        public void ReadSave(PlayerGlobalSaveData data) =>
            _saveData = data.SoundsSaveData;

        public void WriteSave(PlayerGlobalSaveData saveData) =>
            saveData.SoundsSaveData = _saveData;

        public UniTask Initialize()
        {
            var needToSave = _saveData == null;
            _saveData ??= new SoundsSaveData();

            var soundsObject = new GameObject("SoundsService");
            soundsObject.transform.SetParent(_diContainer.DefaultParent);

            InitializeAudioSources(soundsObject);
            InitializeAmbient(soundsObject);
            SetMusicMute(MusicIsMute);
            SetSoundsMute(SoundsIsMute);

            if (needToSave)
            {
                _saveData = new SoundsSaveData();
                return _playerSaveLoadService.Save();
            }

            return UniTask.CompletedTask;
        }

        public void PlayAudio(SoundsGroupName soundsGroupName, AudioClipName audioClipName,
            SoundType soundType = SoundType.Sound)
        {
            AudioClip clip = _soundsStorage.Get(soundsGroupName, audioClipName);
            float volume = _soundsStorage.GetAudioVolume(soundsGroupName, audioClipName);

            if (clip == null)
                return;

            switch (soundType)
            {
                case SoundType.Sound:
                    PlaySound(clip, volume).Forget();
                    break;
                case SoundType.Music:
                    PlayMusic(clip, audioClipName, volume).Forget();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(soundType), soundType, null);
            }
        }

        public UniTask MuteSounds(bool isMute)
        {
            SetSoundsMute(isMute);
            return _playerSaveLoadService.Save();
        }

        public UniTask MuteMusic(bool isMute)
        {
            SetMusicMute(isMute);
            return _playerSaveLoadService.Save();
        }

        private void InitializeAmbient(GameObject soundsObject)
        {
            _ambientAudioSource = soundsObject.AddComponent<AudioSource>();
            _ambientAudioSource.outputAudioMixerGroup = _soundsConfig.MusicAudioMixerGroup;
            _ambientAudioSource.loop = true;
#if UNITY_IOS
            _ambientAudioSource.volume = 0.75f;
#endif
        }

        private void SetSoundsMute(bool isMute)
        {
            foreach (var audioSource in _audioSources)
            {
                audioSource.mute = isMute;
            }

            _saveData.SoundsIsMute = isMute;
        }

        private void SetMusicMute(bool isMute)
        {
            _ambientAudioSource.mute = isMute;
            _saveData.MusicIsMute = isMute;
        }

        private void InitializeAudioSources(GameObject soundsObject)
        {
            for (var i = 0; i < 5; i++)
            {
                var audioSource = soundsObject.AddComponent<AudioSource>();
                audioSource.loop = false;
                _audioSources.Add(audioSource);
                audioSource.outputAudioMixerGroup = _soundsConfig.SfxAudioMixerGroup;
            }
        }

        private AudioSource GetAudioSource()
        {
            AudioSource audioSource = _audioSources.FirstOrDefault(x => x.isPlaying == false);
            if (audioSource == null)
            {
                audioSource = _audioSources.FirstOrDefault();
            }

            return audioSource;
        }

        private UniTask PlaySound(AudioClip clip, float volume = 1)
        {
            var audioSource = GetAudioSource();
            audioSource.volume = volume;
            audioSource.clip = clip;
            audioSource.Play();
            return UniTask.CompletedTask;
        }

        private UniTask PlayMusic(AudioClip clip, AudioClipName audioClipName, float volume)
        {
            if (_ambientAudioSource.clip != null && clip.name == _ambientAudioSource.clip.name)
                return UniTask.CompletedTask;

            _musicsData[audioClipName] = _ambientAudioSource.time;

            _ambientAudioSource.Stop();
            _ambientAudioSource.clip = clip;

            if (_musicsData.TryGetValue(audioClipName, out var playbackPosition))
            {
                if (audioClipName == AudioClipName.MainGamePlayAmbient)
                {
                    if (playbackPosition < clip.length)
                    {
                        _ambientAudioSource.time = playbackPosition;
                    }
                }
            }

            _ambientAudioSource.volume = volume;
            _ambientAudioSource.Play();
            return UniTask.CompletedTask;
        }
    }
}