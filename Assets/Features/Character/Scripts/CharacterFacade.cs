using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(Rigidbody))]
public class CharacterFacade : MonoBehaviour
{
    private const float MinGroundNormalY = 0.5f;
    private const float MaxGroundedVerticalSpeed = 0.1f;
    private const float GroundCheckDistance = 0.15f;
    private const float ShadowHeightOffset = 0.02f;

    public HealthSystem HealthSystem => _healthSystem;
    public Rigidbody Rigidbody => _rigidbody;
    public CharacterAbilitySystem CharacterAbilitySystem => _abilitySystem;
    public CharacterMoveSystem MoveSystem => _moveSystem;
    public DealDamageEffectSystem DamageEffectSystem => _damageEffectSystem;
    public CharacterCameraMoveSystem CameraSystem => _cameraSystem;
    public Transform CameraPivot => _cameraPivot.transform;

    internal GameObject CharacterModel => _characterModel;
    internal Renderer[] MeshRenderers => _meshRenderers;

    [SerializeField] private GameObject _characterModel;
    [SerializeField] private GameObject _cameraPivot;
    [SerializeField] private Renderer[] _meshRenderers;
    [SerializeField] private Transform _shadow;
    [SerializeField] private LayerMask _shadowLayer;

    [Inject] private ICharacterSystemsFactory _systemsFactory;

    private Rigidbody _rigidbody;
    private Collider _collider;
    private CharacterStats _characterStats;
    private PauseEntity _pauseEntity;
    private HealthSystem _healthSystem;
    private CharacterAbilitySystem _abilitySystem;
    private CharacterMoveSystem _moveSystem;
    private CharacterCameraMoveSystem _cameraSystem;
    private DealDamageEffectSystem _damageEffectSystem;

    private void Update()
    {
        UpdateHealth(Time.deltaTime);
        _moveSystem.CaptureJumpInput(Time.deltaTime);
        _moveSystem.UpdateDash(Time.deltaTime);
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

    private void LateUpdate() =>
        UpdateShadow();

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<Wall>() != null ||
            collision.gameObject.GetComponent<Obstacle>() != null)
            _moveSystem.CanMove(true);
    }

    public void Initialize()
    {
        _systemsFactory.Create(this);
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        if (_shadow != null)
            _shadow.SetParent(null);

        _healthSystem.Initialize();
    }

    public void Construct(Rigidbody rigidbody, Collider collider, CharacterStats characterStats,
        PauseEntity pauseEntity, HealthSystem healthSystem, CharacterAbilitySystem abilitySystem,
        CharacterMoveSystem moveSystem, CharacterCameraMoveSystem cameraSystem,
        DealDamageEffectSystem damageEffectSystem)
    {
        _rigidbody = rigidbody;
        _collider = collider;
        _characterStats = characterStats;
        _pauseEntity = pauseEntity;
        _healthSystem = healthSystem;
        _abilitySystem = abilitySystem;
        _moveSystem = moveSystem;
        _cameraSystem = cameraSystem;
        _damageEffectSystem = damageEffectSystem;
    }

    public bool ReceiveDamage(int rawDamage, EnemyFacade source)
    {
        if (_healthSystem.IsDead || rawDamage <= 0)
            return false;

        float evasionChance = Mathf.Clamp(_characterStats.Evasion, 0f, 100f) * 0.01f;
        if (Random.value < evasionChance)
            return false;

        float armorMultiplier = 100f / (100f + Mathf.Max(0f, _characterStats.Armor));
        int reducedDamage = Mathf.Max(1, Mathf.RoundToInt(rawDamage * armorMultiplier));
        int appliedDamage = _healthSystem.GetDamage(reducedDamage);

        if (appliedDamage <= 0)
            return false;

        _damageEffectSystem.DealDamage();

        int thornsDamage = Mathf.Max(0, Mathf.RoundToInt(_characterStats.ThornsDamage));
        if (source != null && thornsDamage > 0)
            source.HealthSystem.GetDamage(thornsDamage);

        return true;
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
            _healthSystem.IncreaseCurrentHealth(Mathf.Max(0f, _characterStats.RegenHp) * deltaTime);
    }

    private void UpdateShadow()
    {
        if (_shadow == null)
            return;

        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity,
                _shadowLayer, QueryTriggerInteraction.Ignore))
            return;

        Vector3 position = transform.position;
        _shadow.position = new Vector3(position.x, hit.point.y + ShadowHeightOffset, position.z);
    }

    private void UpdateGroundedState()
    {
        if (_rigidbody.linearVelocity.y > MaxGroundedVerticalSpeed)
        {
            _moveSystem.SetGrounded(false);
            return;
        }

        Bounds bounds = _collider.bounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f;
        float castDistance = bounds.extents.y - radius + GroundCheckDistance;

        bool isGrounded = Physics.SphereCast(bounds.center, radius, Vector3.down, out RaycastHit hit,
                              castDistance, _shadowLayer, QueryTriggerInteraction.Ignore)
                          && hit.normal.y >= MinGroundNormalY;

        _moveSystem.SetGrounded(isGrounded);

        if (isGrounded)
            _moveSystem.CanMove(true);
    }
}
