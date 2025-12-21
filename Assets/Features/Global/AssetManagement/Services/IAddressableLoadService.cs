using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core
{
    public interface IAddressableLoadService
    {
        UniTask<T> Load<T>(string address, CancellationToken token) where T : class;
        UniTask<AssetContainer<T>> Load<T>(AssetReference assetReference, CancellationToken token) where T : Object;
        void Release<T>(AssetReference assetReference) where T : Object;
        void Release(string address);
    }
}