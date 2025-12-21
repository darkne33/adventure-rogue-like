using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services
{
    [Serializable]
    public class AddressableLoadContainerEffectPlayer : AddressableLoadContainer
    {
        private AssetContainer<GameObject> _assetContainer;

        public EffectPlayer Get() => 
            _assetContainer.Asset.GetComponent<EffectPlayer>();
        
        public async UniTask Load(CancellationToken token) =>
            _assetContainer = await LoadService.Load<GameObject>(_assetReference, token);
        
        public void CleanUp() => 
            LoadService.Release<GameObject>(_assetReference);
    }
}