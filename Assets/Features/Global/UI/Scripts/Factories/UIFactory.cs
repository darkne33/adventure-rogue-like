using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace UI
{
    public class UIFactory : IUIFactory
    {
        public Transform UIRoot { get; private set; }

        [Inject] private DiContainer _container;
        [Inject] private ICameraService _cameraService;
        [Inject] private IPanelStorage _panelStorage;
        [Inject] private IPanelsProvider _panelsProvider;
        
        private readonly UIFactoryConfig _config;

        public UIFactory(UIFactoryConfig config)
        {
            _config = config;
        }

        public async UniTask Initialize(CancellationToken cts)
        {
            await CreateUIRoot(cts);
        }

        private async UniTask CreateUIRoot(CancellationToken cts)
        {
            await _config.UIRoot.Load(cts);
            var root = _container.InstantiatePrefab(_config.UIRoot.Get(), _container.DefaultParent);
            UIRoot = root.transform;
            UIRoot.GetComponent<Canvas>().worldCamera = _cameraService.MainCamera.GetComponent<Camera>();
            _panelsProvider.ConstructPanelLocations(root.GetComponent<PanelLocationsMonoComponent>()
                .PanelLocationRoots);
        }
    }
}