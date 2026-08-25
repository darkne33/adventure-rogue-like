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
    private const float GroundProbeRadiusScale = 0.9f;
    private const int GroundProbeHitCapacity = 16;

    private Rigidbody _rigidbody;
    private Collider _collider;
    private CharacterStats _characterStats;
    private PauseEntity _pauseEntity;
    private HealthSystem _healthSystem;
    private ShieldSystem _shieldSystem;
    private CharacterAbilitySystem _abilitySystem;
    private CharacterMoveSystem _moveSystem;
    private CharacterCameraMoveSystem _cameraSystem;
    private CharacterProximityTransparencySystem _proximityTransparencySystem;
    private DealDamageEffectSystem _damageEffectSystem;
    private IDamageView _damageView;
    private float _invulnerableUntilTime;
    private bool _isTransitionPaused;
    private bool _wasKinematicBeforeControlLock;
    private int _obstacleLayer = -1;
    private int _wallLayer = -1;
    private int _defaultLayer = -1;
    private Color _defaultOutlineColor;
    private bool _isShieldOutlineActive;
    private readonly RaycastHit[] _groundProbeHits = new RaycastHit[GroundProbeHitCapacity];

    private bool IsControlLocked => _isTransitionPaused ||
                                    (_pauseEntity?.IsCinematicPaused ?? false);

    private void Awake()
    {
        _obstacleLayer = LayerMask.NameToLayer("Obstacle");
        _wallLayer = LayerMask.NameToLayer("Wall");
        _defaultLayer = LayerMask.NameToLayer("Default");

        if (_outline != null)
            _defaultOutlineColor = _outline.OutlineColor;
    }

    private void Update()
    {
        UpdateHealth(Time.deltaTime);
        UpdateShield(Time.deltaTime);
        _moveSystem.CaptureJumpInput(Time.deltaTime);
        _moveSystem.Rotate(Time.deltaTime);

        if (_pauseEntity.IsPauseEntity == false)
            _abilitySystem.TickAbilities(this);

        _proximityTransparencySystem.Tick(Time.deltaTime);
        _cameraSystem.Move();
    }

    private void OnDestroy() =>
        _proximityTransparencySystem?.Dispose();

    private void FixedUpdate()
    {
        UpdateGroundedState();
        _moveSystem.Move();
        _moveSystem.Jump();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsObstacleOrWall(collision.gameObject) == false)
            return;

        _moveSystem.CanMove(true);

        // Obstacle tops are valid ground. Only a lateral impact should cancel movement bonuses.
        if (HasWalkableContact(collision) == false)
            _moveSystem.ResetBunnyHopBonus();
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
        CharacterProximityTransparencySystem proximityTransparencySystem,
        DealDamageEffectSystem damageEffectSystem, IDamageView damageView)
    {
        _rigidbody = rigidbody;
        _collider = collider;
        _characterStats = characterStats;
        _pauseEntity = pauseEntity;
        _healthSystem = healthSystem;
        _shieldSystem = shieldSystem;
        _abilitySystem = abilitySystem;
        _moveSystem = moveSystem;
        _cameraSystem = cameraSystem;
        _proximityTransparencySystem = proximityTransparencySystem;
        _damageEffectSystem = damageEffectSystem;
        _damageView = damageView;
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

        if (_relicManager != null && _relicManager.TryBlockIncomingDamage(this))
            return false;

        float armorMultiplier = 100f / (100f + Mathf.Max(0f, _characterStats.Armor));
        int reducedDamage = Mathf.Max(1, Mathf.RoundToInt(rawDamage * armorMultiplier));
        if (_relicManager != null)
            reducedDamage = _relicManager.ModifyIncomingDamage(this, reducedDamage);

        if (reducedDamage <= 0)
            return false;

        int absorbedDamage = _shieldSystem.AbsorbDamage(reducedDamage);
        int healthDamage = reducedDamage - absorbedDamage;

        float healthBeforeDamage = _healthSystem.CurrentHealth;
        if (healthDamage > 0 && healthBeforeDamage - healthDamage <= 0f &&
            _relicManager != null &&
            _relicManager.TryCancelFatalDamage(this, healthDamage))
        {
            int rescuedHealthDamage =
                Mathf.CeilToInt(Mathf.Max(0f, healthBeforeDamage - _healthSystem.CurrentHealth));
            PlayDamageFeedback(absorbedDamage + rescuedHealthDamage);
            _damageEffectSystem.DealDamage();
            return true;
        }

        int appliedHealthDamage = healthDamage > 0 ? _healthSystem.GetDamage(healthDamage) : 0;
        int appliedDamage = absorbedDamage + appliedHealthDamage;

        if (appliedDamage <= 0)
            return false;

        _moveSystem.ResetBunnyHopBonus();
        PlayDamageFeedback(appliedDamage);
        _damageEffectSystem.DealDamage();
        _relicEventBus?.PublishDamageTaken(new RelicDamageTakenEvent(this, source, appliedDamage, "Enemy"));

        int thornsDamage = Mathf.Max(0, Mathf.RoundToInt(_characterStats.ThornsDamage));
        if (source != null && thornsDamage > 0)
            source.HealthSystem.GetDamage(thornsDamage);

        return true;
    }

    private void PlayDamageFeedback(int damage)
    {
        if (damage <= 0)
            return;

        _damageView?.ShowDamage(damage, _healthSystem.MaxHealth, false);
        _cameraSystem.PlayDamageShake();
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

        UpdateShieldOutline();
    }

    private void UpdateShieldOutline()
    {
        if (_outline == null)
            return;

        bool isShieldOutlineActive = _shieldSystem.CurrentShield > 0f;
        if (_isShieldOutlineActive == isShieldOutlineActive)
            return;

        _isShieldOutlineActive = isShieldOutlineActive;
        _outline.OutlineColor = isShieldOutlineActive ? Color.blue : _defaultOutlineColor;
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
        float probeRadius = Mathf.Max(
            Physics.defaultContactOffset,
            Mathf.Min(bounds.extents.x, bounds.extents.z) * GroundProbeRadiusScale);
        Vector3 probeOrigin = bounds.center;

        if (_pivotGroundChecker != null)
        {
            probeOrigin.x = _pivotGroundChecker.position.x;
            probeOrigin.z = _pivotGroundChecker.position.z;
        }

        float castDistance = Mathf.Max(0f, bounds.extents.y - probeRadius) + GroundCheckDistance;
        int fallbackObstacleMask = _defaultLayer >= 0 ? 1 << _defaultLayer : 0;
        int groundProbeMask = _shadowLayer.value | fallbackObstacleMask;
        int hitCount = Physics.SphereCastNonAlloc(
            probeOrigin,
            probeRadius,
            Vector3.down,
            _groundProbeHits,
            castDistance,
            groundProbeMask,
            QueryTriggerInteraction.Ignore);

        float closestDistance = float.PositiveInfinity;
        groundNormal = Vector3.up;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _groundProbeHits[i];
            if (hit.collider == null || hit.rigidbody == _rigidbody ||
                IsGroundSurface(hit.collider) == false ||
                hit.normal.y < MinGroundNormalY || hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            groundNormal = hit.normal.normalized;
        }

        return closestDistance < float.PositiveInfinity;
    }

    private bool IsGroundSurface(Collider surfaceCollider)
    {
        int surfaceLayerMask = 1 << surfaceCollider.gameObject.layer;
        return (_shadowLayer.value & surfaceLayerMask) != 0 ||
               surfaceCollider.GetComponentInParent<Ground>() != null ||
               surfaceCollider.GetComponentInParent<Obstacle>() != null;
    }

    private bool IsObstacleOrWall(GameObject collisionObject)
    {
        int layer = collisionObject.layer;
        return layer == _obstacleLayer ||
               layer == _wallLayer ||
               collisionObject.GetComponentInParent<Obstacle>() != null ||
               collisionObject.GetComponentInParent<Wall>() != null;
    }

    private static bool HasWalkableContact(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y >= MinGroundNormalY)
                return true;
        }

        return false;
    }
}
