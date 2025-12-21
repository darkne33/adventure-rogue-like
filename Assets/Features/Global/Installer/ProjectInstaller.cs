using Core.Services;
using Core.Sounds;
using Infrastructure.SaveSystem;
using Zenject;

namespace Core.Installer
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<AddressableInstaller>();
            
            Container.BindInterfacesAndSelfTo<GameStateMachine>().AsSingle();
            
            Container.Bind<IScenesPreloader>().To<ScenesPreloader>().AsSingle();
            Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
            
            Container.Bind<ISoundsStorage>().To<SoundsStorage>().AsSingle();
            Container.Bind<ISoundsService>().To<SoundsService>().AsSingle();
            Container.Bind<IPlayerSaveLoadService>().To<PlayerSaveLoadService>().AsSingle();
            
            Container.Bind<IDynamicEffectsService>().To<DynamicEffectsService>().AsSingle();
            
            Container.Bind<IGameModeService>().To<GameModeService>().AsSingle();
        }
    }
}