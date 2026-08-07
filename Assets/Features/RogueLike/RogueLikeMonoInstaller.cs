using Core;
using Core.Services;
using Features.Enemies.Scripts;
using Features.Enemies.Scripts.Level.Scripts;
using Features.Leaderboard;
using Features.Relics.Scripts;
using Features.RewardBag;
using UnityEngine;
using Zenject;

public class RogueLikeMonoInstaller : MonoInstaller
{
    [SerializeField] private CharacterConfiguration _characterConfiguration;
    [SerializeField] private CharacterCameraSettingsConfiguration _cameraSettingsConfiguration;
    [SerializeField] private CharacterSettingsConfiguration _characterSettingsConfiguration;
    [SerializeField] private UpgradeOfferConfiguration _upgradeOfferConfiguration;

    [SerializeField] private LevelsConfiguration _levelsConfiguration;
    [SerializeField] private RoomCompletionTimeSlowSettings _roomCompletionTimeSlowSettings = new();

    [SerializeField] private AllAbilitiesConfiguration _abilitiesConfiguration;
    [SerializeField] private RelicPoolConfiguration _relicPoolConfiguration;
    [SerializeField] private RelicChestConfiguration _relicChestConfiguration;
    [SerializeField] private GoldDropperConfiguration _goldDropperConfiguration;
    [SerializeField] private ExpDropperConfiguration _expDropperConfiguration;
    [SerializeField] private HeartDropperConfiguration _heartDropperConfiguration;
    [SerializeField] private GameObject _rewardBagPrefab;

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
        BindRewardBag();
        BindCharacterStats();
        BindUpgradeOffer();
        BindGoldDropper();
        BindExpDropper();
        BindHeartDropper();
        BindRelics();
        BindDebugMode();
    }

    private void BindFactories()
    {
        Container.Bind<ICharacterFactory>().To<CharacterFactory>().AsSingle();
        Container.Bind<ICharacterSystemsFactory>().To<CharacterSystemsFactory>().AsSingle();
        Container.Bind<ILevelFactory>().To<LevelFactory>().AsSingle();
        Container.Bind<IEnemyFactory>().To<EnemyFactory>().AsSingle();
        Container.Bind<IEnemySystemsFactory>().To<EnemySystemsFactory>().AsSingle();
        Container.Bind<IUpgradeOfferItemFactory>().To<UpgradeOfferItemFactory>().AsSingle();
        Container.Bind<MinimapElementFactory>().AsSingle();
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
        Container.Bind<ICharacterAimTargetProvider>().To<CharacterAimTargetProvider>().AsSingle();
        Container.Bind<IRoomTransitionService>().To<RoomTransitionService>().AsSingle();
        Container.Bind<ITransitToRoomService>().To<TransitToRoomService>().AsSingle();
        Container.Bind<RelicChestRollService>().AsSingle();
        Container.BindInterfacesAndSelfTo<LevelProgressionService>().AsSingle();
        Container.BindInterfacesAndSelfTo<MinimapController>().AsSingle();
    }

    private void BindSpawners() => 
        Container.Bind<EnemySpawner>().AsSingle();

    private void BindObservers()
    {
        _roomCompletionTimeSlowSettings ??= new RoomCompletionTimeSlowSettings();

        Container.Bind<RoomCompletionTimeSlowSettings>()
            .FromInstance(_roomCompletionTimeSlowSettings)
            .AsSingle();
        Container.Bind<EnemyRoomObserver>().AsSingle();
        Container.BindInterfacesAndSelfTo<RoomCompletionTimeSlowEffect>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<RoomLeaderboardReporter>().AsSingle().NonLazy();
    }

    private void BindCharacterWallet() =>
        Container.Bind<CharacterWallet>().AsSingle();

    private void BindRewardBag() =>
        Container.Bind<RewardBagSpawner>().AsSingle().WithArguments(_rewardBagPrefab);

    private void BindCharacterStats()
    {
        Container.Bind<CharacterStats>().AsSingle();
        Container.Bind<CharacterStatModifierLayer>().AsSingle();
        Container.Bind<CharacterDamageCalculator>().AsSingle();
    }

    private void BindUpgradeOffer()
    {
        Container.Bind<UpgradeBuildService>().AsSingle();
        Container.BindInterfacesAndSelfTo<UpgradeBuildViewService>().AsSingle();
        Container.Bind<IUpgradeOfferGenerator>().To<UpgradeOfferGenerator>().AsSingle();
        Container.BindInterfacesAndSelfTo<UpgradeOfferHandler>().AsSingle();
    }

    private void BindGoldDropper()
    {
        _goldDropperConfiguration ??= ScriptableObject.CreateInstance<GoldDropperConfiguration>();

        Container.Bind<GoldDropperConfiguration>().FromInstance(_goldDropperConfiguration).AsSingle();
        Container.Bind<GoldDropper>().AsSingle();
    }

    private void BindExpDropper()
    {
        _expDropperConfiguration ??= ScriptableObject.CreateInstance<ExpDropperConfiguration>();

        Container.Bind<ExpDropperConfiguration>().FromInstance(_expDropperConfiguration).AsSingle();
        Container.Bind<ExpDropper>().AsSingle();
    }

    private void BindHeartDropper()
    {
        _heartDropperConfiguration ??= ScriptableObject.CreateInstance<HeartDropperConfiguration>();

        Container.Bind<HeartDropperConfiguration>().FromInstance(_heartDropperConfiguration).AsSingle();
        Container.Bind<HeartDropper>().AsSingle();
    }

    private void BindRelics()
    {
        _relicPoolConfiguration ??= Resources.Load<RelicPoolConfiguration>("Relics/RelicPoolConfiguration");
        _relicChestConfiguration ??= Resources.Load<RelicChestConfiguration>("Relics/RelicChestConfiguration");

        Container.Bind<RelicPoolConfiguration>().FromInstance(_relicPoolConfiguration).AsSingle();
        Container.Bind<RelicChestConfiguration>().FromInstance(_relicChestConfiguration).AsSingle();
        Container.Bind<RelicEventBus>().AsSingle();
        Container.Bind<RelicUnlockService>().AsSingle();
        Container.Bind<RelicPool>().AsSingle();
        Container.Bind<IRelicVisualEffectService>().To<RelicVisualEffectService>().AsSingle();
        Container.BindInterfacesAndSelfTo<RelicManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<RelicChestSpawner>().AsSingle();
        Container.BindInterfacesAndSelfTo<MinimapChestMarkerController>().AsSingle();
        Container.BindInterfacesAndSelfTo<RelicInventoryViewService>().AsSingle();
    }

    private void BindDebugMode() =>
        Container.BindInterfacesAndSelfTo<GameDebugService>().AsSingle().NonLazy();
}
