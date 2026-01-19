using Core;
using Core.Services;
using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

public class RogueLikeMonoInstaller : MonoInstaller
{
    [SerializeField] private CharacterConfiguration _characterConfiguration;
    [SerializeField] private CharacterCameraSettingsConfiguration _cameraSettingsConfiguration;
    [SerializeField] private CharacterSettingsConfiguration _characterSettingsConfiguration;
    
    [SerializeField] private LevelsConfiguration _levelsConfiguration;

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
        Container.Bind<ILevelFactory>().To<LevelFactory>().AsSingle();
    }

    private void BindProviders()
    {
        Container.Bind<ISceneService<RogueLikeSceneProvider>>().To<SceneService<RogueLikeSceneProvider>>().AsSingle()
            .WithArguments(SceneNames.GetSceneNameByType(_sceneNameType));
        Container.Bind<ICharacterProvider>().To<CharacterProvider>().AsSingle();
        Container.Bind<IEnemiesProvider>().To<EnemiesProvider>().AsSingle();
    }

    private void BindConfigurations()
    {
        Container.Bind<CharacterConfiguration>().FromInstance(_characterConfiguration).AsSingle();
        Container.Bind<CharacterCameraSettingsConfiguration>().FromInstance(_cameraSettingsConfiguration).AsSingle();
        Container.Bind<CharacterSettingsConfiguration>().FromInstance(_characterSettingsConfiguration).AsSingle();
        
        Container.Bind<LevelsConfiguration>().FromInstance(_levelsConfiguration).AsSingle();
    }
}