using Core;
using Core.Services;
using UnityEngine;
using Zenject;

public class RogueLikeMonoInstaller : MonoInstaller
{
    [SerializeField] private CharacterConfiguration _characterConfiguration;
    [SerializeField] private CharacterCameraSettingsConfiguration _cameraSettingsConfiguration;
    [SerializeField] private CharacterSettingsConfiguration _characterSettingsConfiguration;

    [SerializeField] private SceneNames.SceneNameType _sceneNameType;
    
    public override void InstallBindings()
    {
        BindProviders();
        BindConfigurations();
        BindFactories();
    }

    private void BindFactories()
    {
        Container.Bind<ICharacterFactory>().To<CharacterFactory>().AsSingle();
    }

    private void BindProviders()
    {
        Container.Bind<ISceneService<RogueLikeSceneProvider>>().To<SceneService<RogueLikeSceneProvider>>().AsSingle()
            .WithArguments(SceneNames.GetSceneNameByType(_sceneNameType));
        Container.Bind<ICharacterProvider>().To<CharacterProvider>().AsSingle();
    }

    private void BindConfigurations()
    {
        Container.Bind<CharacterConfiguration>().FromInstance(_characterConfiguration).AsSingle();
        Container.Bind<CharacterCameraSettingsConfiguration>().FromInstance(_cameraSettingsConfiguration).AsSingle();
        Container.Bind<CharacterSettingsConfiguration>().FromInstance(_characterSettingsConfiguration).AsSingle();
    }
}