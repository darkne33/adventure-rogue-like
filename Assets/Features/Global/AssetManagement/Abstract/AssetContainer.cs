using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Core
{
    public class AssetContainer<TObject> where TObject : Object
    {
        public TObject Asset;

        private readonly AssetReference _reference;

        public AssetContainer(AssetReference loadedReference) =>
            _reference = loadedReference;

        public async UniTask WarmUp(CancellationToken token)
        {
            if (Asset != null)
                return;
            try
            {
                Asset = await Addressables.LoadAssetAsync<TObject>(_reference).ToUniTask(cancellationToken: token);
            }
            catch (Exception e)
            {
                Debug.LogError($"Cant load addressable {_reference.RuntimeKey}.\nError: {e.Message}");
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
                await WarmUp(token);
            }
        }

        public void Cleanup()
        {
            Addressables.Release(Asset);
            Asset = null;
        }
    }
}