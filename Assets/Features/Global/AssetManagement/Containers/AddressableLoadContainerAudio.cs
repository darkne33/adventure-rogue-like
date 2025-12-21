using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class AddressableLoadContainerAudio : AddressableLoadContainer
    {
        private AssetContainer<AudioClip> _assetContainer;

        public AudioClip Get() => 
            _assetContainer.Asset;
        
        public async UniTask Load(CancellationToken token) =>
            _assetContainer = await LoadService.Load<AudioClip>(_assetReference, token);

        public void CleanUp() => 
            LoadService.Release<AudioClip>(_assetReference);
    }
}