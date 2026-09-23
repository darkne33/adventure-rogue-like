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
                throw new ArgumentException("Address is null or empty.", nameof(address));

            token.ThrowIfCancellationRequested();
            if (!_handles.TryGetValue(address, out AsyncOperationHandle handle))
            {
                handle = Addressables.LoadAssetAsync<T>(address);
                _handles.Add(address, handle);
            }

            try
            {
                await handle.ToUniTask(cancellationToken: token);
                return (T)handle.Result;
            }
            catch (OperationCanceledException)
            {
                // Another waiter may still need the same in-flight operation.
                // Ownership stays in the cache until the caller releases it.
                throw;
            }
            catch
            {
                if (_handles.TryGetValue(address, out AsyncOperationHandle cached) && cached.Equals(handle))
                {
                    _handles.Remove(address);
                    if (handle.IsValid())
                        Addressables.Release(handle);
                }
                throw;
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
