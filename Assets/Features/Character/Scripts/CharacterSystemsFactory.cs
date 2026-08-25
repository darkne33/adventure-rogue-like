using System;
using Core;
using Features.Enemies.Scripts;
using UI;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class CharacterSystemsFactory : ICharacterSystemsFactory
{
    private readonly CharacterCameraSettingsConfiguration _cameraSettings;
    private readonly ICameraService _cameraService;
    private readonly IPanelService _panelService;
    private readonly CharacterStats _characterStats;
    private readonly PauseEntityDistributor _pauseEntityDistributor;
    private readonly CharacterConfiguration _characterConfiguration;
    private readonly ISceneService<RogueLikeSceneProvider> _sceneService;

    public CharacterSystemsFactory(CharacterCameraSettingsConfiguration cameraSettings, ICameraService cameraService,
        IPanelService panelService, CharacterStats characterStats, PauseEntityDistributor pauseEntityDistributor,
        CharacterConfiguration characterConfiguration,
        ISceneService<RogueLikeSceneProvider> sceneService)
    {
        _cameraSettings = cameraSettings;
        _cameraService = cameraService;
        _panelService = panelService;
        _characterStats = characterStats;
        _pauseEntityDistributor = pauseEntityDistributor;
        _characterConfiguration = characterConfiguration;
        _sceneService = sceneService;
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
        var proximityTransparencySystem =
            new CharacterProximityTransparencySystem(facade.transform, collider);
        var abilitySystem = new CharacterAbilitySystem();
        var damageEffectSystem = new DealDamageEffectSystem(facade.MeshRenderers);
        IDamageView damageView = facade.GetComponent<CharacterDamageNumberView>();

        CharacterPanel characterPanel = (CharacterPanel)_panelService.GetPanel(PanelName.CharacterPanel);
        Volume globalVolume = _sceneService.GameSceneComponentsService.GlobalVolume;
        if (globalVolume == null)
            throw new InvalidOperationException("Global Volume is not assigned in RogueLikeSceneProvider.");

        LowHealthVignetteView lowHealthVignetteView = globalVolume.GetComponent<LowHealthVignetteView>();
        if (lowHealthVignetteView == null)
            throw new InvalidOperationException("Global Volume is missing LowHealthVignetteView.");

        var deathSystem = new CharacterDeathSystem(facade);
        var healthSystem = new HealthSystem(_characterStats.MaxHp,
            new IHealthView[] { characterPanel.CharacterHealthView, worldHealthView, lowHealthVignetteView },
            deathSystem);
        CharacterSettingsConfiguration settings = _characterConfiguration.CharacterSettings;
        var shieldSystem = new ShieldSystem(settings.ShieldRegenerationDelay,
            settings.ShieldRegenerationPerSecond, characterPanel.CharacterShieldView);

        facade.Construct(rigidbody, collider, _characterStats, pauseEntity, healthSystem, shieldSystem, abilitySystem,
            animationSystem, moveSystem, cameraSystem, proximityTransparencySystem, damageEffectSystem, damageView);
    }
}
