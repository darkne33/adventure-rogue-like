using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class AddressableLoadContainerScriptableObject : AddressableLoadContainer
    {
        private AssetContainer<ScriptableObject> _assetContainer;

        public ScriptableObject Get() =>
            _assetContainer.Asset;
        
        public async UniTask Load(CancellationToken token) =>
            _assetContainer = await LoadService.Load<ScriptableObject>(_assetReference, token);

        public void CleanUp() =>
            LoadService.Release<ScriptableObject>(_assetReference);
    }
}