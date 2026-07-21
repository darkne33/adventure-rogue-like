using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(Rigidbody))]
public class CharacterFacade : MonoBehaviour
{
    public HealthSystem HealthSystem => _healthSystem;
    public ShieldSystem ShieldSystem => _shieldSystem;
    public Rigidbody Rigidbody => _rigidbody;
    public CharacterAbilitySystem CharacterAbilitySystem => _abilitySystem;
    public CharacterMoveSystem MoveSystem => _moveSystem;
    public DealDamageEffectSystem DamageEffectSystem => _damageEffectSystem;
    public CharacterCameraMoveSystem CameraSystem => _cameraSystem;
    public Transform CameraPivot => _cameraPivot.transform;
    public Transform RelicRootTarget => _relicRootTarget;
    public Vector3 ProjectileSpawnPosition =>
        _collider != null ? _collider.bounds.center : transform.position;

    public GameObject CharacterModel => _characterModel;
    public  Renderer[] MeshRenderers => _meshRenderers;
    public  Outline Outline => _outline;
    public  bool IsTransitionPaused => _isTransitionPaused;

    [SerializeField] private GameObject _characterModel;
    [SerializeField] private GameObject _cameraPivot;
    [SerializeField] private Transform _relicRootTarget;
    [SerializeField] private Renderer[] _meshRenderers;
    [SerializeField] private Outline _outline;
    [SerializeField] private Transform _shadow;
    [SerializeField] private LayerMask _shadowLayer;
    [SerializeField] private Transform _pivotGroundChecker;

    [Inject] private ICharacterSystemsFactory _systemsFactory;
    [InjectOptional] private RelicManager _relicManager;
    [InjectOptional] private RelicEventBus _relicEventBus;

    private const float MinGroundNormalY = 0.5f;
    private const float MaxGroundedVerticalSpeed = 0.1f;
    private const float GroundCheckDistance = 0.3f;

    private Rigidbody _rigidbody;
    private Collider _collider;
    private CharacterStats _characterStats;
    private PauseEntity _pauseEntity;
    private HealthSystem _healthSystem;
    private ShieldSystem _shieldSystem;
    private CharacterAbilitySystem _abilitySystem;
    private CharacterAnimationSystem _animationSystem;
    private CharacterMoveSystem _moveSystem;
    private CharacterCameraMoveSystem _cameraSystem;
    private DealDamageEffectSystem _damageEffectSystem;
    private float _invulnerableUntilTime;
    private bool _isTransitionPaused;
    private bool _wasKinematicBeforeControlLock;

    private bool IsControlLocked => _isTransitionPaused ||
                                    (_pauseEntity?.IsCinematicPaused ?? false);

    private void Update()
    {
        UpdateHealth(Time.deltaTime);
        UpdateShield(Time.deltaTime);
        _moveSystem.CaptureJumpInput(Time.deltaTime);
        _moveSystem.Rotate(Time.deltaTime);

        if (_pauseEntity.IsPauseEntity == false)
            _abilitySystem.TickAbilities(this);

        _cameraSystem.Move();
    }

    private void FixedUpdate()
    {
        UpdateGroundedState();
        _moveSystem.Move();
        _moveSystem.Jump();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<Wall>() != null ||
            collision.gameObject.GetComponent<Obstacle>() != null)
        {
            _moveSystem.CanMove(true);
            _moveSystem.ResetBunnyHopBonus();
        }
    }

    public void Initialize()
    {
        _systemsFactory.Create(this);
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        _healthSystem.Initialize();
        _shieldSystem.Initialize(_characterStats.Shield);
    }

    public void Construct(Rigidbody rigidbody, Collider collider, CharacterStats characterStats,
        PauseEntity pauseEntity, HealthSystem healthSystem, ShieldSystem shieldSystem,
        CharacterAbilitySystem abilitySystem, CharacterAnimationSystem animationSystem,
        CharacterMoveSystem moveSystem, CharacterCameraMoveSystem cameraSystem,
        DealDamageEffectSystem damageEffectSystem)
    {
        _rigidbody = rigidbody;
        _collider = collider;
        _characterStats = characterStats;
        _pauseEntity = pauseEntity;
        _healthSystem = healthSystem;
        _shieldSystem = shieldSystem;
        _abilitySystem = abilitySystem;
        _animationSystem = animationSystem;
        _moveSystem = moveSystem;
        _cameraSystem = cameraSystem;
        _damageEffectSystem = damageEffectSystem;
    }

    public bool ReceiveDamage(int rawDamage, EnemyFacade source)
    {
        if (Time.unscaledTime < _invulnerableUntilTime)
            return false;

        if (_healthSystem.IsDead || rawDamage <= 0)
            return false;

        float evasionChance = Mathf.Clamp(_characterStats.Evasion, 0f, 100f) * 0.01f;
        if (Random.value < evasionChance)
            return false;

        float armorMultiplier = 100f / (100f + Mathf.Max(0f, _characterStats.Armor));
        int reducedDamage = Mathf.Max(1, Mathf.RoundToInt(rawDamage * armorMultiplier));
        int absorbedDamage = _shieldSystem.AbsorbDamage(reducedDamage);
        int healthDamage = reducedDamage - absorbedDamage;

        if (healthDamage > 0 && _healthSystem.CurrentHealth - healthDamage <= 0f &&
            _relicManager != null &&
            _relicManager.TryCancelFatalDamage(this, healthDamage))
        {
            _damageEffectSystem.DealDamage();
            return true;
        }

        int appliedHealthDamage = healthDamage > 0 ? _healthSystem.GetDamage(healthDamage) : 0;
        int appliedDamage = absorbedDamage + appliedHealthDamage;

        if (appliedDamage <= 0)
            return false;

        _moveSystem.ResetBunnyHopBonus();
        _damageEffectSystem.DealDamage();
        _relicEventBus?.PublishDamageTaken(new RelicDamageTakenEvent(this, source, appliedDamage, "Enemy"));

        int thornsDamage = Mathf.Max(0, Mathf.RoundToInt(_characterStats.ThornsDamage));
        if (source != null && thornsDamage > 0)
            source.HealthSystem.GetDamage(thornsDamage);

        return true;
    }

    public void SetTemporaryInvulnerability(float duration) =>
        _invulnerableUntilTime = Mathf.Max(_invulnerableUntilTime, Time.unscaledTime + duration);

    public void SetTransitionPaused(bool state)
    {
        if (_isTransitionPaused == state || _rigidbody == null)
            return;

        bool wasControlLocked = IsControlLocked;
        _isTransitionPaused = state;
        _pauseEntity.SetTransitionPaused(state);
        _moveSystem.SetTransitionPaused(state);
        RefreshControlLock(wasControlLocked);
    }

    internal void SetCinematicPaused(bool state)
    {
        if (_pauseEntity == null || _rigidbody == null || _pauseEntity.IsCinematicPaused == state)
            return;

        bool wasControlLocked = IsControlLocked;
        _pauseEntity.SetCinematicPaused(state);
        RefreshControlLock(wasControlLocked);
    }

    private void RefreshControlLock(bool wasControlLocked)
    {
        bool isControlLocked = IsControlLocked;
        _cameraSystem.SetInputEnabled(isControlLocked == false);

        if (wasControlLocked == isControlLocked)
            return;

        if (isControlLocked)
        {
            _wasKinematicBeforeControlLock = _rigidbody.isKinematic;
            if (_rigidbody.isKinematic == false)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            _rigidbody.isKinematic = true;
            return;
        }

        _rigidbody.isKinematic = _wasKinematicBeforeControlLock;
        if (_rigidbody.isKinematic == false)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        _moveSystem.CanMove(true);
    }

    public void DisableAfterDeath()
    {
        enabled = false;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        _collider.enabled = false;

        if (_shadow != null)
            Destroy(_shadow.gameObject);

        Destroy(gameObject);
    }

    private void UpdateHealth(float deltaTime)
    {
        _healthSystem.SetMaxHealth(_characterStats.MaxHp);

        if (_pauseEntity.IsPauseEntity == false)
        {
            float healed = _healthSystem.IncreaseCurrentHealth(Mathf.Max(0f, _characterStats.RegenHp) * deltaTime);
            if (healed > 0f)
                _relicEventBus?.PublishHeal(new RelicHealEvent(this, healed));
        }
    }

    private void UpdateShield(float deltaTime)
    {
        _shieldSystem.SetMaxShield(_characterStats.Shield);

        if (_pauseEntity.IsPauseEntity == false)
            _shieldSystem.Tick(deltaTime);
    }

    private void UpdateGroundedState()
    {
        bool hasGroundSurface = TryGetGroundSurface(out Vector3 groundNormal);

        float surfaceSeparationSpeed = hasGroundSurface
            ? Vector3.Dot(_rigidbody.linearVelocity, groundNormal)
            : float.PositiveInfinity;
        bool isGrounded = hasGroundSurface &&
                          surfaceSeparationSpeed <= MaxGroundedVerticalSpeed;

        _moveSystem.SetGrounded(isGrounded, hasGroundSurface ? groundNormal : Vector3.up);

        if (isGrounded)
            _moveSystem.CanMove(true);
    }

    private bool TryGetGroundSurface(out Vector3 groundNormal)
    {
        Bounds bounds = _collider.bounds;
        Vector3 footPosition = _pivotGroundChecker != null
            ? _pivotGroundChecker.position
            : new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 rayOrigin = footPosition + Vector3.up * GroundCheckDistance;
        float rayDistance = GroundCheckDistance * 2f;

        bool hasGround = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            rayDistance,
            _shadowLayer,
            QueryTriggerInteraction.Ignore)
            && hit.normal.y >= MinGroundNormalY;

        groundNormal = hasGround ? hit.normal.normalized : Vector3.up;
        return hasGround;
    }
}
