using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Sounds
{
    public interface ISoundsStorage
    {
        UniTask InitializeAndWarmup(CancellationToken cts);
        AudioClip Get(SoundsGroupName soundsGroupName, AudioClipName audioClipName);
        float GetAudioVolume(SoundsGroupName soundsGroupName, AudioClipName audioClipName);
        void Remove(SoundsGroupName soundsGroupName);
        void Add(SoundAddressableGroup soundsGroupData);
    }
}