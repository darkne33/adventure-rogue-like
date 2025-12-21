using System;
using Cysharp.Threading.Tasks;
using ModestTree;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core
{
    public class GameAddressableService : IGameAddressableService
    {
        public async UniTask InitializeAddressables()
        {
            await Addressables.InitializeAsync();
            await CheckCatalogs();
        }

        private async UniTask CheckCatalogs()
        {
            try
            {
                var checkForUpdateHandle = Addressables.CheckForCatalogUpdates();
                var catalogUpdates = await checkForUpdateHandle.Task;

                if (catalogUpdates.Count > 0)
                {
                    await Addressables.UpdateCatalogs(catalogUpdates);

                    Debug.Log($"[Entry] Load remote catalogs: {catalogUpdates.Count}");
                }
                else
                {
                    Debug.Log("[Entry] Cached catalogs are used");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Entry] {exception.Message}");
            }
        }
    }
}