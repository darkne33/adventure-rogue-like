using Core;
using Features.Enemies.Scripts;
using UI;
using UnityEngine;

public sealed class CharacterSystemsFactory : ICharacterSystemsFactory
{
    private readonly CharacterCameraSettingsConfiguration _cameraSettings;
    private readonly ICameraService _cameraService;
    private readonly IPanelService _panelService;
    private readonly CharacterStats _characterStats;
    private readonly PauseEntityDistributor _pauseEntityDistributor;
    private readonly CharacterSettingsConfiguration _characterSettingsConfiguration;
    private readonly CharacterChestOpeningService _chestOpeningService;

    public CharacterSystemsFactory(CharacterCameraSettingsConfiguration cameraSettings, ICameraService cameraService,
        IPanelService panelService, CharacterStats characterStats, PauseEntityDistributor pauseEntityDistributor,
        CharacterSettingsConfiguration characterSettingsConfiguration,
        CharacterChestOpeningService chestOpeningService)
    {
        _cameraSettings = cameraSettings;
        _cameraService = cameraService;
        _panelService = panelService;
        _characterStats = characterStats;
        _pauseEntityDistributor = pauseEntityDistributor;
        _characterSettingsConfiguration = characterSettingsConfiguration;
        _chestOpeningService = chestOpeningService;
    }

    public void Create(CharacterFacade facade)
    {
        Rigidbody rigidbody = facade.GetComponent<Rigidbody>();
        Collider collider = facade.GetComponent<Collider>();
        CharacterFxSystem fxSystem = facade.CharacterModel.GetComponent<CharacterFxSystem>();
        HealthView worldHealthView = facade.GetComponent<HealthView>();
        Animator animator = facade.CharacterModel.GetComponent<Animator>();
        PauseEntity pauseEntity = _pauseEntityDistributor.EntityDistribute();

        var animationSystem = new CharacterAnimationSystem(animator);
        var cameraSystem = new CharacterCameraMoveSystem(
            facade.CameraPivot, _cameraSettings, _cameraService, pauseEntity);
        var moveSystem = new CharacterMoveSystem(rigidbody, _cameraService, _characterStats, fxSystem,
            facade.CharacterModel, animationSystem, cameraSystem, pauseEntity);
        var abilitySystem = new CharacterAbilitySystem();
        var damageEffectSystem = new DealDamageEffectSystem(facade.MeshRenderers);
        IDamageView damageView = facade.GetComponent<CharacterDamageNumberView>();

        CharacterPanel characterPanel = (CharacterPanel)_panelService.GetPanel(PanelName.CharacterPanel);
        var deathSystem = new CharacterDeathSystem(facade);
        var healthSystem = new HealthSystem(_characterStats.MaxHp,
            new IHealthView[] { characterPanel.CharacterHealthView, worldHealthView }, deathSystem);
        var shieldSystem = new ShieldSystem(_characterSettingsConfiguration.ShieldRegenerationDelay,
            _characterSettingsConfiguration.ShieldRegenerationPerSecond, characterPanel.CharacterShieldView);

        facade.Construct(rigidbody, collider, _characterStats, pauseEntity, healthSystem, shieldSystem, abilitySystem,
            animationSystem, moveSystem, cameraSystem, damageEffectSystem, damageView);
        _chestOpeningService.Initialize(facade, rigidbody, pauseEntity, animationSystem);
    }
}
