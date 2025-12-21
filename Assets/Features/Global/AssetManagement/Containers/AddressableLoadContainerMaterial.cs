using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class AddressableLoadContainerMaterial : AddressableLoadContainer
    {
        private AssetContainer<Material> _assetContainer;

        public Material Get() =>
            _assetContainer.Asset;

        public async UniTask Load(CancellationToken token) =>
            _assetContainer = await LoadService.Load<Material>(_assetReference, token);

        public void CleanUp() =>
            LoadService.Release<Material>(_assetReference);
    }
}