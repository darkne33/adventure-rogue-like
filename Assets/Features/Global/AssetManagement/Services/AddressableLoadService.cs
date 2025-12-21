using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace Core
{
    [Preserve]
    public class AddressableLoadService : IAddressableLoadService
    {
        private readonly Dictionary<AssetReference, AssetContainersData> _assetContainers = new();
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new();

        private class AssetContainersData
        {
            public object container;
            public int referenceCount;
        }

        public async UniTask<AssetContainer<T>> Load<T>(AssetReference assetReference, CancellationToken token)
            where T : Object
        {
            if (_assetContainers.TryGetValue(assetReference, out var containerStruct))
            {
                var existContainer = (AssetContainer<T>)containerStruct.container;
                containerStruct.referenceCount++;
                await existContainer.WarmUp(token);
                return existContainer;
            }

            var assetContainer = new AssetContainer<T>(assetReference);
            _assetContainers.Add(assetReference, new AssetContainersData
            {
                container = assetContainer,
                referenceCount = 1,
            });
            await assetContainer.WarmUp(token);
            return assetContainer;
        }

        public void Release<T>(AssetReference assetReference) where T : Object
        {
            if (_assetContainers.TryGetValue(assetReference, out var containerStruct))
            {
                containerStruct.referenceCount--;
                if (containerStruct.referenceCount == 0)
                {
                    var existContainer = (AssetContainer<T>)containerStruct.container;
                    existContainer.Cleanup();
                    _assetContainers.Remove(assetReference);
                }
            }
        }

        public async UniTask<T> Load<T>(string address, CancellationToken token) where T : class
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("Address is null or empty");
                return null;
            }

            try
            {
                if (_handles.ContainsKey(address))
                {
                    await _handles[address].Task;
                    return (T)_handles[address].Result;
                }

                var asyncOperationHandle = Addressables.LoadAssetAsync<T>(address);
                _handles.Add(address, asyncOperationHandle);
                await asyncOperationHandle.ToUniTask(cancellationToken: token);
                return asyncOperationHandle.Result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Cant load addressable by address {e.Message}");
                _handles.Remove(address);
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
                return await Load<T>(address, token);
            }
        }

        public void Release(string address)
        {
            if (_handles.Remove(address, out AsyncOperationHandle handle))
            {
                Addressables.Release(handle);
            }
        }
    }
}