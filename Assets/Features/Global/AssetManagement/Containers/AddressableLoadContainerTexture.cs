using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class AddressableLoadContainerTexture: AddressableLoadContainer
    {
        private AssetContainer<Texture> _assetContainer;

        public Texture Get() => 
            _assetContainer.Asset;
        
        public async UniTask Load(CancellationToken token) => 
            _assetContainer = await LoadService.Load<Texture>(_assetReference, token);

        public void CleanUp() => 
            LoadService.Release<Texture>(_assetReference);
    }
}