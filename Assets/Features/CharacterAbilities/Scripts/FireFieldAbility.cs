using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public sealed class FireFieldAbility : CharacterActiveAbility
{
    private const float MaxTrackedMovementDelta = 15f;
    private const string DamageStatName = "Damage";
    private const string DistanceStatName = "Distance";
    private const string RadiusStatName = "Radius";

    private static readonly AbilityUpgradeType[] FireFieldUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.FireFieldDistance,
        AbilityUpgradeType.FireFieldRadius
    };

    private static readonly AbilityUpgradeType[] FireFieldUpgradeTypesAtMinimumDistance =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.FireFieldRadius
    };

    private readonly IEnemiesProvider _enemiesProvider;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _characterStats;
    private readonly RelicEventBus _relicEventBus;
    private readonly RelicManager _relicManager;
    private readonly LevelsConfiguration _levelsConfiguration;

    private FireFieldAbilityConfiguration _configuration;
    private int _damage;
    private float _distancePerField;
    private float _damageRadius;
    private float _distanceSinceLastField;
    private Vector3 _lastCharacterPosition;
    private bool _hasLastCharacterPosition;

    public override AbilityUpgradeType[] UpgradeTypes =>
        _configuration != null &&
        _distancePerField <= _configuration.MinimumDistancePerField + Mathf.Epsilon
            ? FireFieldUpgradeTypesAtMinimumDistance
            : FireFieldUpgradeTypes;

    public FireFieldAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
        CharacterStats characterStats, RelicEventBus relicEventBus, RelicManager relicManager,
        LevelsConfiguration levelsConfiguration)
    {
        _enemiesProvider = enemiesProvider;
        _damageCalculator = damageCalculator;
        _characterStats = characterStats;
        _relicEventBus = relicEventBus;
        _relicManager = relicManager;
        _levelsConfiguration = levelsConfiguration;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        _configuration = (FireFieldAbilityConfiguration)abilityConfig;
        Level = 0;
        Cooldown = 0f;
        _damage = 0;
        _distancePerField = _configuration.DistancePerField;
        _damageRadius = _configuration.DamageRadius;
        _distanceSinceLastField = 0f;
        _hasLastCharacterPosition = false;
        StatName_1 = DamageStatName;
        StatName_2 = DistanceStatName;
        Stat_1 = _configuration.StartDamage;
        Stat_2 = _distancePerField;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);

        if (_damage <= 0)
            _damage = _configuration.StartDamage;

        switch (CurrentUpgradeType)
        {
            case AbilityUpgradeType.Damage:
                _damage += GetDamageIncrease(CurrentUpgradeMultiplier);
                break;
            case AbilityUpgradeType.FireFieldDistance:
                ReduceDistancePerField(CurrentUpgradeMultiplier);
                break;
            case AbilityUpgradeType.FireFieldRadius:
                _damageRadius += GetRadiusIncrease(CurrentUpgradeMultiplier);
                break;
        }

        if (Level == 1)
        {
            _distanceSinceLastField = 0f;
            _hasLastCharacterPosition = false;
        }

        Stat_1 = _damage;
        Stat_2 = _distancePerField;
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        _distanceSinceLastField = 0f;
        _hasLastCharacterPosition = false;
    }

    public override float GetStatFromIncrease() =>
        _damage;

    public override float GetStatToIncrease(float upgradeMultiplier) =>
        GetDamageTo(upgradeMultiplier);

    public override AbilityUpgradePreview[] GetAcquirePreviews() =>
        new[]
        {
            new AbilityUpgradePreview(DamageStatName, _configuration.StartDamage),
            new AbilityUpgradePreview(DistanceStatName, _configuration.DistancePerField, "m")
        };

    public override AbilityUpgradePreview[] GetUpgradePreviews(AbilityUpgradeType upgradeType,
        float upgradeMultiplier)
    {
        return upgradeType switch
        {
            AbilityUpgradeType.FireFieldDistance => new[]
            {
                new AbilityUpgradePreview(DistanceStatName, _distancePerField,
                    GetDistanceTo(upgradeMultiplier), "m")
            },
            AbilityUpgradeType.FireFieldRadius => new[]
            {
                new AbilityUpgradePreview(RadiusStatName, _damageRadius,
                    _damageRadius + GetRadiusIncrease(upgradeMultiplier), "m")
            },
            _ => new[]
            {
                new AbilityUpgradePreview(DamageStatName, _damage, GetDamageTo(upgradeMultiplier))
            }
        };
    }

    protected override void OnUse(CharacterFacade character)
    {
        if (character == null || _configuration.Prefab == null)
            return;

        TrackMovement(character);
    }

    private void TrackMovement(CharacterFacade character)
    {
        Vector3 currentPosition = character.transform.position;

        if (_hasLastCharacterPosition == false)
        {
            _lastCharacterPosition = currentPosition;
            _hasLastCharacterPosition = true;
            return;
        }

        Vector3 segmentStart = _lastCharacterPosition;
        Vector3 planarMovement = currentPosition - segmentStart;
        planarMovement.y = 0f;
        float movementDistance = planarMovement.magnitude;
        _lastCharacterPosition = currentPosition;

        if (movementDistance <= 0.001f || movementDistance > MaxTrackedMovementDelta)
            return;

        float distancePerField = Mathf.Max(0.1f, _distancePerField);
        float processedDistance = 0f;

        while (_distanceSinceLastField + movementDistance - processedDistance >= distancePerField)
        {
            float distanceToNextField = distancePerField - _distanceSinceLastField;
            processedDistance += distanceToNextField;
            float segmentProgress = processedDistance / movementDistance;
            SpawnField(character, Vector3.Lerp(segmentStart, currentPosition, segmentProgress));
            _distanceSinceLastField = 0f;
        }

        _distanceSinceLastField += movementDistance - processedDistance;
    }

    private void SpawnField(CharacterFacade character, Vector3 position)
    {
        if (TryGetGroundPosition(position, out Vector3 groundPosition, out Vector3 groundNormal) == false)
            return;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, groundNormal);
        GameObject fieldObject = Object.Instantiate(_configuration.Prefab, groundPosition, rotation);
        FireFieldDamageArea damageArea = fieldObject.GetComponent<FireFieldDamageArea>();

        if (damageArea == null)
        {
            Debug.LogError($"{_configuration.Prefab.name} is missing {nameof(FireFieldDamageArea)}.");
            Object.Destroy(fieldObject);
            return;
        }

        float duration = _configuration.FieldDuration * AbilityDurationMultiplier;
        damageArea.Initialize(_enemiesProvider, _damageRadius, _configuration.DamageHeight,
            _configuration.DamageTickInterval, duration, enemy => ApplyDamage(character, enemy));
    }

    private bool TryGetGroundPosition(Vector3 position, out Vector3 groundPosition, out Vector3 groundNormal)
    {
        Vector3 rayOrigin = position + Vector3.up * Mathf.Max(0f, _configuration.GroundRayStartHeight);
        float rayDistance = Mathf.Max(0.1f, _configuration.GroundRayDistance);
        LayerMask groundLayer = _levelsConfiguration != null && _levelsConfiguration.GroundLayer.value != 0
            ? _levelsConfiguration.GroundLayer
            : Physics.DefaultRaycastLayers;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal.normalized;
            groundPosition = hit.point + groundNormal * _configuration.GroundOffset;
            return true;
        }

        groundPosition = default;
        groundNormal = Vector3.up;
        return false;
    }

    private void ApplyDamage(CharacterFacade character, EnemyFacade enemy)
    {
        if (character == null || enemy == null || enemy.IsDead)
            return;

        Vector3 hitPosition = enemy.transform.position;
        CharacterDamageResult damageResult = _damageCalculator.Calculate(GetRolledDamage());
        int finalDamage = _relicManager.ModifyOutgoingDamage(damageResult.Damage, enemy);
        int appliedDamage = enemy.HealthSystem.GetDamage(finalDamage, damageResult.IsCritical);
        bool killedByHit = enemy.IsDead;

        if (appliedDamage <= 0)
            return;

        float lifeStealPercent = Mathf.Max(0f, _characterStats.LifeSteal) * 0.01f;
        float healed = character.HealthSystem.IncreaseCurrentHealth(appliedDamage * lifeStealPercent);
        if (healed > 0f)
            _relicEventBus.PublishHeal(new RelicHealEvent(character, healed));

        enemy.EffectsSystem.DealDamage();
        _relicEventBus.PublishHit(new RelicHitEvent(character, enemy, appliedDamage,
            damageResult.IsCritical, Id.ToString(), hitPosition));

        if (killedByHit)
            _relicEventBus.PublishKill(new RelicKillEvent(character, enemy, hitPosition, Id.ToString()));
    }

    private int GetRolledDamage()
    {
        float variation = Mathf.Max(0f, _configuration.DamageVariationPercent) * 0.01f;
        float multiplier = Random.Range(1f - variation, 1f + variation);
        return Mathf.Max(1, Mathf.RoundToInt(_damage * multiplier));
    }

    private int GetDamageIncrease(float upgradeMultiplier) =>
        Mathf.Max(0, Mathf.RoundToInt(GetUpgradeValue(_configuration.DamageUpgradeIncrease, upgradeMultiplier)));

    private int GetDamageTo(float upgradeMultiplier) =>
        (_damage <= 0 ? _configuration.StartDamage : _damage) + GetDamageIncrease(upgradeMultiplier);

    private float GetDistanceTo(float upgradeMultiplier) =>
        Mathf.Max(_configuration.MinimumDistancePerField,
            _distancePerField - GetDistanceReduction(upgradeMultiplier));

    private float GetDistanceReduction(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.DistanceUpgradeReduction, upgradeMultiplier);

    private void ReduceDistancePerField(float upgradeMultiplier)
    {
        float previousDistance = Mathf.Max(0.1f, _distancePerField);
        float travelProgress = Mathf.Clamp01(_distanceSinceLastField / previousDistance);
        _distancePerField = GetDistanceTo(upgradeMultiplier);
        _distanceSinceLastField = travelProgress * _distancePerField;
    }

    private float GetRadiusIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.RadiusUpgradeIncrease, upgradeMultiplier);
}
