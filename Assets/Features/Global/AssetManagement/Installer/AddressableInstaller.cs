namespace Core
{
    public class AddressableInstaller : Zenject.Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<IAddressableLoadService>().To<AddressableLoadService>().AsSingle();
            Container.Bind<IGameAddressableService>().To<GameAddressableService>().AsSingle();
        }
    }
}