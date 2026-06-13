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

    public CharacterSystemsFactory(CharacterCameraSettingsConfiguration cameraSettings, ICameraService cameraService,
        IPanelService panelService, CharacterStats characterStats, PauseEntityDistributor pauseEntityDistributor)
    {
        _cameraSettings = cameraSettings;
        _cameraService = cameraService;
        _panelService = panelService;
        _characterStats = characterStats;
        _pauseEntityDistributor = pauseEntityDistributor;
    }

    public void Create(CharacterFacade facade)
    {
        Rigidbody rigidbody = facade.GetComponent<Rigidbody>();
        Collider collider = facade.GetComponent<Collider>();
        CharacterFxSystem fxSystem = facade.GetComponent<CharacterFxSystem>();
        HealthView worldHealthView = facade.GetComponent<HealthView>();
        Animator animator = facade.GetComponent<Animator>();
        PauseEntity pauseEntity = _pauseEntityDistributor.EntityDistribute();

        var animationSystem = new CharacterAnimationSystem(animator);
        var moveSystem = new CharacterMoveSystem(rigidbody, _cameraService, _characterStats, fxSystem,
            facade.CharacterModel, animationSystem, pauseEntity);
        var abilitySystem = new CharacterAbilitySystem();
        var cameraSystem = new CharacterCameraMoveSystem(facade.CameraPivot, _cameraSettings, pauseEntity);
        var damageEffectSystem = new DealDamageEffectSystem(facade.MeshRenderers);

        CharacterPanel characterPanel = (CharacterPanel)_panelService.GetPanel(PanelName.CharacterPanel);
        var deathSystem = new CharacterDeathSystem(facade);
        var healthSystem = new HealthSystem(_characterStats.MaxHp,
            new IHealthView[] { characterPanel.CharacterHealthView, worldHealthView }, deathSystem);

        facade.Construct(rigidbody, collider, _characterStats, pauseEntity, healthSystem, abilitySystem, moveSystem,
            cameraSystem, damageEffectSystem);
    }
}
