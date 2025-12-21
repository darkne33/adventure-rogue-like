using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace UI
{
    public class StoragePanelData
    {
        public PanelBase PanelPrefab;
        public PanelData PanelData;
        public readonly DiContainer Container;

        private readonly IAddressableLoadService _addressableLoadService;

        public StoragePanelData(PanelData panelData, DiContainer container,
            IAddressableLoadService addressableLoadService)
        {
            PanelData = panelData;
            Container = container;
            _addressableLoadService = addressableLoadService;
        }

        public async UniTask LoadPanel(CancellationToken token)
        {
            var container = await _addressableLoadService.Load<GameObject>(PanelData.AssetReference, token);
            PanelPrefab = container.Asset.GetComponent<PanelBase>();
        }

        public void Release() =>
            _addressableLoadService.Release<GameObject>(PanelData.AssetReference);
    }
}