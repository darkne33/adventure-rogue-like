using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class AddressableLoadContainerGameObject : AddressableLoadContainer
    {
        private AssetContainer<GameObject> _assetContainer;

        public GameObject Get() => 
            _assetContainer.Asset;
        
        public async UniTask Load(CancellationToken token) =>
            _assetContainer = await LoadService.Load<GameObject>(_assetReference, token);

        public void CleanUp() => 
            LoadService.Release<GameObject>(_assetReference);
    }
}