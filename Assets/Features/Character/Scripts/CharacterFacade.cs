using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(Rigidbody))]
public class CharacterFacade : MonoBehaviour
{
    public CharacterDefinition Definition { get; internal set; }
    public HealthSystem HealthSystem => _healthSystem;
    public ShieldSystem ShieldSystem => _shieldSystem;
    public Rigidbody Rigidbody => _rigidbody;
    public Collider Collider => _collider;
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
    public  Outline[] Outlines => _outlines;
    public  bool IsTransitionPaused => _isTransitionPaused;
    public bool IsCinematicPaused => _pauseEntity?.IsCinematicPaused ?? false;

    [SerializeField] private GameObject _characterModel;
    [SerializeField] private GameObject _cameraPivot;
    [SerializeField] private Transform _relicRootTarget;
    [SerializeField] private Renderer[] _meshRenderers;
    [SerializeField] private Outline[] _outlines = System.Array.Empty<Outline>();
    [SerializeField] private Transform _shadow;
    [SerializeField] private LayerMask _shadowLayer;
    [SerializeField] private Transform _pivotGroundChecker;

    [Inject] private ICharacterSystemsFactory _systemsFactory;
    [Inject] private AllAbilitiesConfiguration _abilitiesConfiguration;
    [InjectOptional] private RelicManager _relicManager;
    [InjectOptional] private RelicEventBus _relicEventBus;
    [Inject] private IRogueLikeRuntimeDataService _runtimeDataService;
    [Inject] private Features.Sounds.ISoundsService _sounds;

    public void PlayCombatSound(Features.Sounds.SoundId sound) => _sounds.Play(sound);

    // Normals must point upward: slopes below 90 degrees, with a small floating-point tolerance.
    private const float MinGroundNormalY = 0.00001f;
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
    private CharacterInvulnerabilitySystem _invulnerabilitySystem;
    private IDamageView _damageView;
    private bool _isTransitionPaused;
    private bool _wasKinematicBeforeControlLock;
    private int _obstacleLayer = -1;
    private int _wallLayer = -1;
    private int _defaultLayer = -1;
    private Color[] _defaultOutlineColors;
    private Color _shieldOutlineColor = Color.blue;
    private bool _isShieldOutlineActive;
    private bool _hasGroundContact;
    private Vector3 _groundContactNormal;
    private readonly RaycastHit[] _groundProbeHits = new RaycastHit[GroundProbeHitCapacity];

    private bool IsControlLocked => _isTransitionPaused ||
                                    (_pauseEntity?.IsCinematicPaused ?? false);

    private void Awake()
    {
        _obstacleLayer = LayerMask.NameToLayer("Obstacle");
        _wallLayer = LayerMask.NameToLayer("Wall");
        _defaultLayer = LayerMask.NameToLayer("Default");

        if (_outlines == null)
            _outlines = System.Array.Empty<Outline>();

        _defaultOutlineColors = new Color[_outlines.Length];
        for (int i = 0; i < _outlines.Length; i++)
        {
            if (_outlines[i] != null)
                _defaultOutlineColors[i] = _outlines[i].OutlineColor;
        }
    }

    private void Update()
    {
        _invulnerabilitySystem.Tick(Time.deltaTime, _pauseEntity.IsPauseEntity);

        UpdateHealth(Time.deltaTime);
        UpdateShield(Time.deltaTime);
        _moveSystem.CaptureJumpInput(Time.deltaTime);
        _moveSystem.Rotate(Time.deltaTime);

        if (_pauseEntity.IsPauseEntity == false)
            _abilitySystem.TickAbilities(this);

        _proximityTransparencySystem.Tick(Time.deltaTime);
        _cameraSystem.Move();
    }

    private void OnDisable() =>
        _invulnerabilitySystem?.Reset();

    private void OnDestroy()
    {
        if (_healthSystem != null)
            _healthSystem.DamageReceived -= HandleHealthDamageReceived;
        _abilitySystem?.Dispose();
        _invulnerabilitySystem?.Dispose();
        _moveSystem?.Dispose();
        _cameraSystem?.Dispose();
        _proximityTransparencySystem?.Dispose();
    }

    private void FixedUpdate()
    {
        UpdateGroundedState();
        _moveSystem.Move();
        _moveSystem.Jump();
    }

    private void OnCollisionEnter(Collision collision)
    {
        RecordGroundContacts(collision);

        if (IsObstacleOrWall(collision.gameObject) == false)
            return;

        _moveSystem.CanMove(true);

        // Obstacle tops are valid ground. Only a lateral impact should cancel movement bonuses.
        if (HasWalkableContact(collision) == false)
            _moveSystem.ResetBunnyHopBonus();
    }

    private void OnCollisionStay(Collision collision) =>
        RecordGroundContacts(collision);

    public void Initialize()
    {
        ConfigureShieldOutlineColor();
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
        DealDamageEffectSystem damageEffectSystem, IDamageView damageView,
        CharacterInvulnerabilitySystem invulnerabilitySystem)
    {
        _rigidbody = rigidbody;
        _collider = collider;
        _characterStats = characterStats;
        _pauseEntity = pauseEntity;
        if (_healthSystem != null)
            _healthSystem.DamageReceived -= HandleHealthDamageReceived;
        _healthSystem = healthSystem;
        _healthSystem.DamageReceived += HandleHealthDamageReceived;
        _shieldSystem = shieldSystem;
        _abilitySystem = abilitySystem;
        _moveSystem = moveSystem;
        _cameraSystem = cameraSystem;
        _proximityTransparencySystem = proximityTransparencySystem;
        _damageEffectSystem = damageEffectSystem;
        _damageView = damageView;
        _invulnerabilitySystem = invulnerabilitySystem;
    }

    public bool TryBlockProjectile(Vector3 from, Vector3 to, float radius, CombatTarget source,
        out float hitDistance)
    {
        hitDistance = 0f;
        return isActiveAndEnabled && _pauseEntity?.IsPauseEntity == false &&
               _healthSystem?.IsDead == false && _abilitySystem != null &&
               _abilitySystem.TryBlockProjectile(this, source, from, to, radius, out hitDistance);
    }

    public bool ReceiveAreaDamage(float damage, CombatTarget source, Vector3 from, Vector3 to, float radius)
    {
        if (_pauseEntity.IsPauseEntity || _invulnerabilitySystem.IsActive || _healthSystem.IsDead || damage <= 0f)
            return false;
        if (TryBlockProjectile(from, to, radius, source, out _))
            return false;
        return ReceiveDamage(damage, source, allowAegisBlock: false);
    }

    public bool ReceiveBoxDamage(float damage, CombatTarget source, Vector3 center,
        Vector3 halfExtents, Quaternion rotation)
    {
        if (_pauseEntity.IsPauseEntity || _invulnerabilitySystem.IsActive || _healthSystem.IsDead || damage <= 0f)
            return false;
        if (_abilitySystem.TryBlockBox(this, source, center, halfExtents, rotation))
            return false;
        return ReceiveDamage(damage, source, allowAegisBlock: false);
    }

    public bool ReceiveDamage(float rawDamage, CombatTarget source, bool allowAegisBlock = true,
        Vector3? damageOrigin = null)
    {
        if (_pauseEntity.IsPauseEntity || _invulnerabilitySystem.IsActive)
            return false;

        if (_healthSystem.IsDead || rawDamage <= 0)
            return false;

        if (allowAegisBlock && _abilitySystem.TryBlockIncomingDamage(this, source, damageOrigin))
            return false;

        float evasionChance = Mathf.Clamp(_characterStats.Evasion, 0f, 100f) * 0.01f;
        if (Random.value < evasionChance)
        {
            _sounds.Play(Features.Sounds.SoundId.PlayerDodge);
            return false;
        }

        if (_relicManager != null && _relicManager.TryBlockIncomingDamage(this))
        {
            GrantDamageInvulnerability(CharacterHealthRules.HalfHeart);
            _sounds.Play(Features.Sounds.SoundId.PlayerBlock);
            return false;
        }

        if (_runtimeDataService.CurrentIndexLevel >= 6)
            rawDamage = Mathf.Max(1f, rawDamage);

        float armorMultiplier = 100f / (100f + Mathf.Max(0f, _characterStats.Armor));
        float reducedDamage = rawDamage * armorMultiplier;
        if (_relicManager != null)
            reducedDamage = _relicManager.ModifyIncomingDamage(this, reducedDamage);

        if (reducedDamage <= 0)
            return false;

        reducedDamage = CharacterHealthRules.NormalizeDamage(reducedDamage);
        float shieldBeforeDamage = _shieldSystem.CurrentShield;
        float absorbedDamage = _shieldSystem.AbsorbDamage(reducedDamage);
        if (absorbedDamage > 0)
            _sounds.Play(_shieldSystem.CurrentShield <= 0f && shieldBeforeDamage > 0f
                ? Features.Sounds.SoundId.PlayerShieldBreak : Features.Sounds.SoundId.PlayerBlock);
        float healthDamage = reducedDamage - absorbedDamage;

        float healthBeforeDamage = _healthSystem.CurrentHealth;
        if (healthDamage > 0 && healthBeforeDamage - healthDamage <= 0f &&
            _relicManager != null &&
            _relicManager.TryCancelFatalDamage(this, healthDamage))
        {
            float rescuedHealthDamage =
                Mathf.Max(0f, healthBeforeDamage - _healthSystem.CurrentHealth);
            HandleHealthDamageReceived(rescuedHealthDamage);
            GrantDamageInvulnerability(reducedDamage);
            _moveSystem.ResetBunnyHopBonus();
            PlayDamageFeedback(absorbedDamage + rescuedHealthDamage);
            _damageEffectSystem.DealDamage();
            _relicEventBus?.PublishDamageTaken(new RelicDamageTakenEvent(this, source,
                absorbedDamage + rescuedHealthDamage, "Enemy"));
            return true;
        }

        float appliedHealthDamage = healthDamage > 0f ? _healthSystem.GetDamage(healthDamage) : 0f;
        if (appliedHealthDamage > 0 && !_healthSystem.IsDead)
            _sounds.Play(Features.Sounds.SoundId.PlayerHit);
        float appliedDamage = absorbedDamage + appliedHealthDamage;

        if (appliedDamage <= 0)
            return false;

        GrantDamageInvulnerability(reducedDamage);
        _moveSystem.ResetBunnyHopBonus();
        PlayDamageFeedback(appliedDamage);
        _damageEffectSystem.DealDamage();
        _relicEventBus?.PublishDamageTaken(new RelicDamageTakenEvent(this, source, appliedDamage, "Enemy"));

        int thornsDamage = Mathf.Max(0, Mathf.RoundToInt(_characterStats.ThornsDamage));
        if (source != null && source.CanBeAttacked && thornsDamage > 0)
        {
            int appliedThornsDamage = source.HealthSystem.GetDamage(thornsDamage);
            if (appliedThornsDamage > 0)
                _abilitySystem.ApplyThornsKnockback(this, source);
        }

        return true;
    }

    public bool ReceiveDirectDamage(float damage, string damageType)
    {
        if (_pauseEntity.IsPauseEntity || _invulnerabilitySystem.IsActive)
            return false;

        if (_healthSystem.IsDead || damage <= 0)
            return false;

        float appliedDamage = _healthSystem.GetDamage(damage);
        if (appliedDamage <= 0)
            return false;

        GrantDamageInvulnerability(appliedDamage);
        if (!_healthSystem.IsDead)
            _sounds.Play(Features.Sounds.SoundId.PlayerHit);
        PlayDamageFeedback(appliedDamage);
        _damageEffectSystem.DealDamage();
        _relicEventBus?.PublishDamageTaken(new RelicDamageTakenEvent(this, null, appliedDamage, damageType));
        return true;
    }

    private void HandleHealthDamageReceived(float amount)
    {
        if (amount > 0f)
            _relicEventBus?.PublishHealthDamageTaken(new RelicHealthDamageTakenEvent(this, amount));
    }

    private void PlayDamageFeedback(float damage)
    {
        if (damage <= 0)
            return;

        _damageView?.ShowDamage(damage, _healthSystem.MaxHealth, false);
        _cameraSystem.PlayDamageShake();
    }

    public void SetTemporaryInvulnerability(float duration)
    {
        if (!_healthSystem.IsDead)
            _invulnerabilitySystem.Grant(duration);
    }

    private void GrantDamageInvulnerability(float damage)
    {
        if (!_healthSystem.IsDead)
            _invulnerabilitySystem.GrantAfterDamage(damage);
    }

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
        _healthSystem.SetMaxHealth(_characterStats.MaxHp, healIncrease: false);

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
        if (_outlines.Length == 0)
            return;

        bool isShieldOutlineActive = _shieldSystem.CurrentShield > 0f;
        if (_isShieldOutlineActive == isShieldOutlineActive)
            return;

        _isShieldOutlineActive = isShieldOutlineActive;
        for (int i = 0; i < _outlines.Length; i++)
        {
            if (_outlines[i] != null)
                _outlines[i].OutlineColor = isShieldOutlineActive
                    ? _shieldOutlineColor
                    : _defaultOutlineColors[i];
        }
    }

    private void ConfigureShieldOutlineColor()
    {
        if (_abilitiesConfiguration?.Abilities == null)
            return;

        foreach (AbilityConfiguration abilityConfiguration in _abilitiesConfiguration.Abilities)
        {
            if (abilityConfiguration is ShieldScrollAbilityConfiguration shieldConfiguration &&
                shieldConfiguration.AbilityName == AbilityName.ShieldScroll)
            {
                _shieldOutlineColor = shieldConfiguration.ShieldColor;
                return;
            }
        }
    }

    private void UpdateGroundedState()
    {
        bool hasGroundSurface = TryGetGroundSurface(out Vector3 groundNormal);
        _hasGroundContact = false;

        float surfaceSeparationSpeed = hasGroundSurface
            ? Vector3.Dot(_rigidbody.linearVelocity, groundNormal)
            : float.PositiveInfinity;
        // Jump clears IsGrounded. Reattach only while descending, even beside an uphill slope.
        bool canLand = _rigidbody.linearVelocity.y <= MaxGroundedVerticalSpeed &&
                       surfaceSeparationSpeed <= MaxGroundedVerticalSpeed;
        bool isGrounded = hasGroundSurface &&
                          (_moveSystem.IsGrounded || canLand);

        _moveSystem.SetGrounded(isGrounded, hasGroundSurface ? groundNormal : Vector3.up);

        if (isGrounded)
            _moveSystem.CanMove(true);
    }

    private bool TryGetGroundSurface(out Vector3 groundNormal)
    {
        if (_hasGroundContact)
        {
            groundNormal = _groundContactNormal;
            return true;
        }

        Bounds bounds = _collider.bounds;
        float bodyRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);
        float probeRadius = Mathf.Max(
            Physics.defaultContactOffset,
            bodyRadius * GroundProbeRadiusScale);

        // A downward-only probe can miss almost vertical slopes. Follow the last support normal.
        if (_moveSystem.IsGrounded)
        {
            Vector3 lowerSphereCenter = bounds.center -
                                        Vector3.up * Mathf.Max(0f, bounds.extents.y - bodyRadius);
            float surfaceProbeDistance = Mathf.Max(0f, bodyRadius - probeRadius) + GroundCheckDistance;
            if (ProbeGroundSurface(lowerSphereCenter, probeRadius, -_moveSystem.GroundNormal,
                    surfaceProbeDistance, out groundNormal))
                return true;
        }

        Vector3 probeOrigin = bounds.center;

        if (_pivotGroundChecker != null)
        {
            probeOrigin.x = _pivotGroundChecker.position.x;
            probeOrigin.z = _pivotGroundChecker.position.z;
        }

        float castDistance = Mathf.Max(0f, bounds.extents.y - probeRadius) + GroundCheckDistance;
        return ProbeGroundSurface(probeOrigin, probeRadius, Vector3.down, castDistance, out groundNormal);
    }

    private bool ProbeGroundSurface(Vector3 origin, float radius, Vector3 direction,
        float distance, out Vector3 groundNormal)
    {
        int fallbackObstacleMask = _defaultLayer >= 0 ? 1 << _defaultLayer : 0;
        int groundProbeMask = _shadowLayer.value | fallbackObstacleMask;
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            radius,
            direction,
            _groundProbeHits,
            distance,
            groundProbeMask,
            QueryTriggerInteraction.Ignore);

        float closestDistance = float.PositiveInfinity;
        groundNormal = Vector3.up;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _groundProbeHits[i];
            if (hit.collider == null || hit.rigidbody == _rigidbody ||
                IsGroundSurface(hit.collider) == false || hit.distance >= closestDistance ||
                !TryGetGroundNormal(hit.collider, hit.point, hit.normal, out Vector3 surfaceNormal))
                continue;

            closestDistance = hit.distance;
            groundNormal = surfaceNormal;
        }

        return closestDistance < float.PositiveInfinity;
    }

    private void RecordGroundContacts(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            Collider surfaceCollider = contact.otherCollider;
            if (surfaceCollider == null || IsGroundSurface(surfaceCollider) == false ||
                !TryGetGroundNormal(surfaceCollider, contact.point, contact.normal, out Vector3 normal))
                continue;

            if (!_hasGroundContact || normal.y > _groundContactNormal.y)
            {
                _hasGroundContact = true;
                _groundContactNormal = normal;
            }
        }
    }

    private static bool TryGetGroundNormal(Collider surfaceCollider, Vector3 point,
        Vector3 contactNormal, out Vector3 groundNormal)
    {
        // Sphere casts can report a rounded edge normal. Read the actual face to reject walls.
        float offset = Mathf.Max(Physics.defaultContactOffset, surfaceCollider.contactOffset) * 2f;
        Ray ray = new(point + contactNormal * offset, -contactNormal);
        if (surfaceCollider.Raycast(ray, out RaycastHit surfaceHit, offset * 2f) &&
            surfaceHit.normal.y > MinGroundNormalY)
        {
            groundNormal = surfaceHit.normal.normalized;
            return true;
        }

        groundNormal = Vector3.up;
        return false;
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
            if (collision.GetContact(i).normal.y > MinGroundNormalY)
                return true;
        }

        return false;
    }
}
