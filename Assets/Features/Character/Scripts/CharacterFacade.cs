using Core;
using Features.Enemies.Scripts;
using UI;
using Unity.VisualScripting;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(Rigidbody))]
public class CharacterFacade : MonoBehaviour
{
    public HealthSystem HealthSystem => _healthSystem;
    public Rigidbody Rigidbody => _rigidbody;
    public CharacterAbilitySystem CharacterAbilitySystem => _characterAbilitySystem;
    public CharacterMoveSystem MoveSystem => _moveSystem;
    public DealDamageEffectSystem DamageEffectSystem => _damageEffectSystem;
    public Transform CameraPivot => _cameraPivot.transform;

    [SerializeField] private GameObject _characterModel;
    [SerializeField] private GameObject _cameraPivot;

    [SerializeField] private Renderer[] _meshRenderers;
    [SerializeField] private Transform _shadow;
    
    [SerializeField] private LayerMask _shadowLayer;
    private static readonly Vector3 OFFSET_SHADOW = new(0, 0.02f);


    [Inject] private CharacterCameraSettingsConfiguration _characterCameraSettingsConfiguration;

    [Inject] private ICameraService _cameraService;
    [Inject] private IPanelService _panelService;
    [Inject] private IAbilityChoiceProvider _abilityChoiceProvider;
    [Inject] private CharacterStats _characterStats;

    private CharacterMoveSystem _moveSystem;
    private CharacterCameraMoveSystem _cameraSystem;
    private HealthSystem _healthSystem;
    private CharacterFxSystem _characterFxSystem;
    private IHealthView _healthView;
    private CharacterAbilitySystem _characterAbilitySystem;
    private IDeathSystem _deathSystem;
    private CharacterGoldView _characterGoldView;
    private CharacterAnimationSystem _characterAnimationSystem;
    private DealDamageEffectSystem _damageEffectSystem;
    
    private Rigidbody _rigidbody;
    private Animator _animator;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _characterFxSystem = GetComponent<CharacterFxSystem>();
        _healthView = GetComponent<HealthView>();
        _animator = GetComponent<Animator>();

        _characterAnimationSystem = new CharacterAnimationSystem(_animator);

        _moveSystem =
            new CharacterMoveSystem(_rigidbody, _cameraService, _characterStats, _characterFxSystem,
                _characterModel, _characterAnimationSystem);

        _characterAbilitySystem = new CharacterAbilitySystem();
        _characterAbilitySystem.AddAbility(_abilityChoiceProvider.GetAbility(AbilityName.RabbitBoomerang), _characterStats);

        CharacterPanel characterPanel = (CharacterPanel)_panelService.GetPanel(PanelName.CharacterPanel);
        _characterGoldView = characterPanel.CharacterGoldView;
        _healthSystem = new HealthSystem(_characterStats.MaxHp,
            new[] { characterPanel.CharacterHealthView, _healthView }, _deathSystem
        );

        _healthSystem.Initialize();

        _cameraSystem = new CharacterCameraMoveSystem(_cameraPivot.transform,
            _characterCameraSettingsConfiguration);

        _damageEffectSystem = new DealDamageEffectSystem(_meshRenderers);

        _shadow.parent = null;
    }

    private void Update()
    {
        _moveSystem.UpdateDash(Time.deltaTime);
        _characterAbilitySystem.TickAbilities(this);
        _cameraSystem.Move();
    }

    private void FixedUpdate()
    {
        _moveSystem.Move();
        _moveSystem.Jump();
        _moveSystem.Rotate();
        CalculateShadow();
    }

    private void OnCollisionEnter(Collision other)
    {
        var ground = other.gameObject.GetComponent<Ground>();
        if (ground != null)
        {
            _moveSystem.ResetCanJump();
            _moveSystem.CanMove(true);
        }

        var wall = other.gameObject.GetComponent<Wall>();
        if (wall != null)
            _moveSystem.CanMove(true);

        var obstacle = other.gameObject.GetComponent<Obstacle>();
        if (obstacle != null)
            _moveSystem.CanMove(true);
    }
    

    private void CalculateShadow()
    {
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out var hit, _shadowLayer))
            _shadow.position = hit.point + OFFSET_SHADOW;
    }
}