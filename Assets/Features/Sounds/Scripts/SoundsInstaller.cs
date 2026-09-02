using Zenject;

namespace Features.Sounds
{
    public sealed class SoundsInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<ISoundSettingsStorage>()
                .To<PlayerPrefsSoundSettingsStorage>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<SoundsService>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("Sounds")
                .AsSingle()
                .NonLazy();
        }
    }
}
