using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace UI
{
    public class PanelsFactory : IPanelsFactory
    {
        [Inject] private IPanelStorage _panelStorage;
        [Inject] private IPanelsProvider _panelsProvider;

        public async UniTask<T> CreatePanel<T>(PanelName panelName) where T : PanelBase
        {
            var storageData = await _panelStorage.Get(panelName, CancellationToken.None);
            var panelLocation = _panelStorage.GetPanelLocation(panelName);
            var root = _panelsProvider.GetRootFor(panelLocation);
            return storageData.Container.InstantiatePrefabForComponent<T>(storageData.PanelPrefab, root);
        }

        public void ClosePanel(PanelBase panel)
        {
            var panelName = panel.PanelName;
            Object.Destroy(panel.gameObject);
            _panelStorage.CleanPanel(panelName);
        }
    }
}