using Core;
using Core.Services;
using Features.Enemies.Scripts;
using Features.Enemies.Scripts.Level.Scripts;
using UnityEngine;
using Zenject;

public class RogueLikeMonoInstaller : MonoInstaller
{
    [SerializeField] private CharacterConfiguration _characterConfiguration;
    [SerializeField] private CharacterCameraSettingsConfiguration _cameraSettingsConfiguration;
    [SerializeField] private CharacterSettingsConfiguration _characterSettingsConfiguration;
    [SerializeField] private UpgradeOfferConfiguration _upgradeOfferConfiguration;

    [SerializeField] private LevelsConfiguration _levelsConfiguration;

    [SerializeField] private AllAbilitiesConfiguration _abilitiesConfiguration;

    [SerializeField] private SceneNames.SceneNameType _sceneNameType;

    public override void InstallBindings()
    {
        BindProviders();
        BindConfigurations();
        BindFactories();
        BindServices();
        BindSpawners();
        BindObservers();
        BindCharacterWallet();
        BindCharacterStats();
        BindUpgradeOffer();
    }

    private void BindFactories()
    {
        Container.Bind<ICharacterFactory>().To<CharacterFactory>().AsSingle();
        Container.Bind<ILevelFactory>().To<LevelFactory>().AsSingle();
        Container.Bind<IEnemyFactory>().To<EnemyFactory>().AsSingle();
        Container.Bind<IUpgradeOfferItemFactory>().To<UpgradeOfferItemFactory>().AsSingle();
    }

    private void BindProviders()
    {
        Container.Bind<ISceneService<RogueLikeSceneProvider>>().To<SceneService<RogueLikeSceneProvider>>().AsSingle()
            .WithArguments(SceneNames.GetSceneNameByType(_sceneNameType));
        Container.Bind<ICharacterProvider>().To<CharacterProvider>().AsSingle();
        Container.Bind<IEnemiesProvider>().To<EnemiesProvider>().AsSingle();
        Container.Bind<IAbilityChoiceProvider>().To<CharacterAbilityChoiceProvider>().AsSingle();
    }

    private void BindConfigurations()
    {
        Container.Bind<CharacterConfiguration>().FromInstance(_characterConfiguration).AsSingle();
        Container.Bind<CharacterCameraSettingsConfiguration>().FromInstance(_cameraSettingsConfiguration).AsSingle();
        Container.Bind<CharacterSettingsConfiguration>().FromInstance(_characterSettingsConfiguration).AsSingle();
        Container.Bind<UpgradeOfferConfiguration>().FromInstance(_upgradeOfferConfiguration).AsSingle();

        Container.Bind<LevelsConfiguration>().FromInstance(_levelsConfiguration).AsSingle();

        Container.Bind<AllAbilitiesConfiguration>().FromInstance(_abilitiesConfiguration).AsSingle();
    }

    private void BindServices()
    {
        Container.Bind<IRogueLikeRuntimeDataService>().To<RogueLikeRuntimeDataService>().AsSingle();
        Container.Bind<ITransitToRoomService>().To<TransitToRoomService>().AsSingle();
    }

    private void BindSpawners() => 
        Container.Bind<EnemySpawner>().AsSingle();

    private void BindObservers() => 
        Container.Bind<EnemiesWaveObserver>().AsSingle();

    private void BindCharacterWallet() =>
        Container.Bind<CharacterWallet>().AsSingle();

    private void BindCharacterStats()
    {
        Container.Bind<CharacterStats>().AsSingle();
        Container.Bind<CharacterDamageCalculator>().AsSingle();
    }

    private void BindUpgradeOffer()
    {
        Container.Bind<IUpgradeOfferGenerator>().To<UpgradeOfferGenerator>().AsSingle();
        Container.Bind<IUpgradeOfferHandler>().To<UpgradeOfferHandler>().AsSingle();
    }
}
