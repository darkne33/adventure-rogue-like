using UnityEngine;
using Zenject;

namespace Core
{
    public class CameraInstaller : MonoInstaller
    {
        [SerializeField] private CameraFactoryConfig _cameraFactoryConfig;

        public override void InstallBindings()
        {
            Container.Bind<CameraFactoryConfig>().FromInstance(_cameraFactoryConfig).AsSingle();
            Container.Bind<ICameraService>().To<CameraService>().AsSingle();
        }
    }
}