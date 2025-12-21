using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class AddressableLoadContainerSprite : AddressableLoadContainer
    {
        private AssetContainer<Sprite> _assetContainer;

        public Sprite Get() =>
            _assetContainer.Asset;
        
        public async UniTask Load(CancellationToken token) => 
            _assetContainer = await LoadService.Load<Sprite>(_assetReference, token);

        public void CleanUp() =>
            LoadService.Release<Sprite>(_assetReference);
    }
}