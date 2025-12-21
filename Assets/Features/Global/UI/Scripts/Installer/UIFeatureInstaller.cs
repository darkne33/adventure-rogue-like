using UnityEngine;
using Zenject;

namespace UI
{
    public class UIFeatureInstaller : MonoInstaller
    {
        [SerializeField] private PanelsConfig _panelsConfig;
        
        public override void InstallBindings()
        {
            //feature
            Container.Bind<IPanelService>().To<PanelService>().FromNew().AsSingle();
            Container.Bind<IPanelsFactory>().To<PanelsFactory>().AsSingle();
            Container.Bind<IPanelPresentersFactory>().To<PanelPresentersFactory>().AsSingle();
            Container.Bind<IPanelPresentersService>().To<PanelPresentersService>().AsSingle();
            Container.Bind<IPanelStorage>().To<PanelStorage>().AsSingle().WithArguments(_panelsConfig);
        }
    }
}