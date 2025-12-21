using UnityEngine;
using Zenject;

namespace UI
{
    public class UIGlobalInstaller : MonoInstaller
    {
        [SerializeField] private UIFactoryConfig _uiFactoryConfig;

        public override void InstallBindings()
        {
            Container.Bind<IUIFactory>().To<UIFactory>().AsSingle().WithArguments(_uiFactoryConfig);
            Container.Bind<IPanelsProvider>().To<PanelsProvider>().AsSingle();
        }
    }
}