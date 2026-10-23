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
                .AsSingle()
                .NonLazy();

            // Destroy playback before GameplayAssetService releases its Addressables.
            Container.BindExecutionOrder<SoundsService>(50);
        }
    }
}
