using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UnityEngine.Scripting;
using Zenject;

namespace UI
{
    [Preserve]
    public sealed class PanelStorage : IPanelStorage
    {
        [Inject] private IPanelPresentersService _panelPresentersService;
        [Inject] private IAddressableLoadService _loadService;
        [Inject] private DiContainer _container;

        private readonly Dictionary<PanelName, StoragePanelData> _storagePanels = new();

        private readonly List<PanelData> _panelsStaticData;

        public PanelStorage(PanelsConfig panelsStatic)
        {
            _panelsStaticData = panelsStatic.GamePanelsAssetReferences;
        }

        public UniTask WarmUp(CancellationToken cts) =>
            UniTask.WhenAll(Enumerable.Select(_panelsStaticData, panelData => Add(panelData, _container, cts)));

        public async UniTask Add(PanelData panelData, DiContainer container, CancellationToken token)
        {
            Log.Addressables.Debug($"Add {panelData.PanelName} to storage");
            var storagePanelData = new StoragePanelData(panelData, container, _loadService);
            if (_storagePanels.ContainsKey(panelData.PanelName))
            {
                Log.Addressables.Warn($"Force cleanup panel {panelData.PanelName} from already loaded");
                CleanPanel(panelData.PanelName);
                Remove(panelData.PanelName);
            }

            _storagePanels.Add(panelData.PanelName, storagePanelData);
            _panelPresentersService.Add(panelData.PanelName, container);

            if (panelData.IsLazy == false)
            {
                await storagePanelData.LoadPanel(token);
            }
        }

        public async UniTask<StoragePanelData> Get(PanelName panelName, CancellationToken token)
        {
            var data = _storagePanels[panelName];
            if (data.PanelData.IsLazy)
            {
                await data.LoadPanel(token);
            }

            return data;
        }

        public void CleanPanel(PanelName panelName)
        {
            if (_storagePanels.TryGetValue(panelName, out var storagePanel))
            {
                if (storagePanel.PanelData.IsLazy)
                    storagePanel.Release();
            }
        }

        public void Remove(PanelName panelName)
        {
            Log.Addressables.Debug($"Remove {panelName} from storage");
            if (_storagePanels.TryGetValue(panelName, out var storagePanel))
            {
                storagePanel.Release();
            }

            _storagePanels.Remove(panelName);
            _panelPresentersService.Remove(panelName);
        }

        public PanelLocation GetPanelLocation(PanelName panelName) =>
            _storagePanels[panelName].PanelData.PanelLocation;

        public bool Exist(PanelName panelName) => 
            _storagePanels.ContainsKey(panelName);
    }
}