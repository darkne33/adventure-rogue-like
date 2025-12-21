using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Package.Logging;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UnityEngine;
using Zenject;

namespace Core.Sounds
{
    public class SoundsStorage : ISoundsStorage
    {
        [Inject] private SoundsConfig _soundsConfig;

        private readonly Dictionary<SoundsGroupName, GroupSoundsConfig> _musicClips = new();

        public async UniTask InitializeAndWarmup(CancellationToken cts)
        {
            await UniTask.WhenAll(Enumerable.Select(_soundsConfig.AudioGroupContainers, pair => pair.Value.Load(cts)));
            foreach (var pair in _soundsConfig.AudioGroupContainers)
            {
                _musicClips.Add(pair.Key, (GroupSoundsConfig)pair.Value.Get());
            }
        }

        public void Add(SoundAddressableGroup soundsGroupData) =>
            _musicClips[soundsGroupData.SoundsGroupName] = (GroupSoundsConfig)soundsGroupData.Config.Get();

        public void Remove(SoundsGroupName soundsGroupName) =>
            _musicClips.Remove(soundsGroupName);

        public AudioClip Get(SoundsGroupName soundsGroupName, AudioClipName audioClipName)
        {
            if (_musicClips.TryGetValue(soundsGroupName, out var groupSoundsConfig))
            {
                return groupSoundsConfig.AudioClips[audioClipName];
            }
            else
            {
                Log.Audio.Error($"Cant found {soundsGroupName} with {audioClipName}");
                return null;
            }
        }
        
        public float GetAudioVolume(SoundsGroupName soundsGroupName, AudioClipName audioClipName)
        {
            if (_musicClips.TryGetValue(soundsGroupName, out var groupSoundsConfig) && groupSoundsConfig.Volumes.TryGetValue(audioClipName, out var volume))
            {
                return volume;
            }
            else
            {
                Log.Audio.Warn($"Cant found volume with {audioClipName}");
                return 1;
            }
        }
    }
}