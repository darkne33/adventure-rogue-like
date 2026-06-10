using System;
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
    public CharacterCameraMoveSystem CameraSystem => _cameraSystem;
    public Transform CameraPivot => _cameraPivot.transform;

    [SerializeField] private GameObject _characterModel;
    [SerializeField] private GameObject _cameraPivot;

    [SerializeField] private Renderer[] _meshRenderers;
    [SerializeField] private Transform _shadow;

    [SerializeField] private LayerMask _shadowLayer;
    private static readonly Vector3 OFFSET_SHADOW = new(0, 0.02f);
    private const float MIN_GROUND_NORMAL_Y = 0.5f;
    private const float MAX_GROUNDED_VERTICAL_SPEED = 0.1f;
    private const float GROUND_CHECK_DISTANCE = 0.15f;


    [Inject] private CharacterCameraSettingsConfiguration _characterCameraSettingsConfiguration;

    [Inject] private ICameraService _cameraService;
    [Inject] private IPanelService _panelService;
    [Inject] private IAbilityChoiceProvider _abilityChoiceProvider;
    [Inject] private CharacterStats _characterStats;
    [Inject] private PauseEntityDistributor _pauseEntityDistributor;

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
    private Collider _collider;
    private Animator _animator;

    private void Update()
    {
        _moveSystem.CaptureJumpInput(Time.deltaTime);
        _moveSystem.UpdateDash(Time.deltaTime);
        _moveSystem.Rotate(Time.deltaTime);
        _characterAbilitySystem.TickAbilities(this);
        _cameraSystem.Move();
    }

    private void FixedUpdate()
    {
        UpdateGroundedState();
        _moveSystem.Move();
        _moveSystem.Jump();
    }

    private void LateUpdate()
    {
        CalculateShadow();
    }

    private void OnCollisionEnter(Collision other)
    {
        var wall = other.gameObject.GetComponent<Wall>();
        if (wall != null)
            _moveSystem.CanMove(true);

        var obstacle = other.gameObject.GetComponent<Obstacle>();
        if (obstacle != null)
            _moveSystem.CanMove(true);
    }

    public void Initialize()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _collider = GetComponent<Collider>();
        _characterFxSystem = GetComponent<CharacterFxSystem>();
        _healthView = GetComponent<HealthView>();
        _animator = GetComponent<Animator>();

        PauseEntity pauseEntity = _pauseEntityDistributor.EntityDistribute();

        _characterAnimationSystem = new CharacterAnimationSystem(_animator);

        _moveSystem =
            new CharacterMoveSystem(_rigidbody, _cameraService, _characterStats, _characterFxSystem,
                _characterModel, _characterAnimationSystem, pauseEntity);

        CharacterPanel characterPanel = (CharacterPanel)_panelService.GetPanel(PanelName.CharacterPanel);
        _characterGoldView = characterPanel.CharacterGoldView;

        _healthSystem = new HealthSystem(_characterStats.MaxHp,
            new[] { characterPanel.CharacterHealthView, _healthView }, _deathSystem
        );

        _characterAbilitySystem = new CharacterAbilitySystem();

        _cameraSystem = new CharacterCameraMoveSystem(_cameraPivot.transform,
            _characterCameraSettingsConfiguration, pauseEntity);

        _damageEffectSystem = new DealDamageEffectSystem(_meshRenderers);

        _shadow.parent = null;

        _healthSystem.Initialize();
    }

    private void CalculateShadow()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out var hit, Mathf.Infinity, _shadowLayer,
                QueryTriggerInteraction.Ignore))
            return;

        Vector3 characterPosition = transform.position;
        _shadow.position = new Vector3(characterPosition.x, hit.point.y + OFFSET_SHADOW.y, characterPosition.z);
    }

    private void UpdateGroundedState()
    {
        if (_rigidbody.linearVelocity.y > MAX_GROUNDED_VERTICAL_SPEED)
        {
            _moveSystem.SetGrounded(false);
            return;
        }

        Bounds bounds = _collider.bounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f;
        float castDistance = bounds.extents.y - radius + GROUND_CHECK_DISTANCE;

        bool isGrounded = Physics.SphereCast(bounds.center, radius, Vector3.down, out RaycastHit hit,
                              castDistance, _shadowLayer, QueryTriggerInteraction.Ignore)
                          && hit.normal.y >= MIN_GROUND_NORMAL_Y;

        _moveSystem.SetGrounded(isGrounded);

        if (isGrounded)
            _moveSystem.CanMove(true);
    }
}
