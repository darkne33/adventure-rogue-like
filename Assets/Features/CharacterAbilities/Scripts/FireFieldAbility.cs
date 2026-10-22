using System.Collections.Generic;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using Features.Sounds;
using UnityEngine;

public sealed class FireFieldAbility : CharacterActiveAbility
{
    private const float MaxTrackedMovementDelta = 15f;
    private const string DamageStatName = "Damage";
    private const string DurationStatName = "Duration";
    private const string RadiusStatName = "Radius";

    private static readonly AbilityUpgradeType[] FireFieldUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.FireFieldDuration,
        AbilityUpgradeType.FireFieldRadius
    };

    private readonly List<FireFieldDamageArea> _activeFields = new();
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _characterStats;
    private readonly RelicEventBus _relicEventBus;
    private readonly RelicManager _relicManager;
    private readonly LevelsConfiguration _levelsConfiguration;

    private FireFieldAbilityConfiguration _configuration;
    private SoundLoopEmitter _burningSound;
    private int _damage;
    private float _distancePerField;
    private float _fieldDuration;
    private float _damageRadius;
    private float _distanceSinceLastField;
    private Vector3 _lastCharacterPosition;
    private bool _hasLastCharacterPosition;

    public override AbilityUpgradeType[] UpgradeTypes => FireFieldUpgradeTypes;

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
        _fieldDuration = _configuration.FieldDuration;
        _damageRadius = _configuration.DamageRadius;
        _distanceSinceLastField = 0f;
        _hasLastCharacterPosition = false;
        StatName_1 = DamageStatName;
        StatName_2 = DurationStatName;
        Stat_1 = _configuration.StartDamage;
        Stat_2 = _fieldDuration;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);

        if (_damage <= 0)
            _damage = _configuration.StartDamage;

        ApplyUpgradeEffect(CurrentPrimaryUpgrade);
        if (CurrentSecondaryUpgrade.HasValue)
            ApplyUpgradeEffect(CurrentSecondaryUpgrade.Value);

        if (Level == 1)
        {
            _distanceSinceLastField = 0f;
            _hasLastCharacterPosition = false;
        }

        Stat_1 = _damage;
        Stat_2 = _fieldDuration;
    }

    private void ApplyUpgradeEffect(AbilityUpgradeEffect upgrade)
    {
        switch (upgrade.Type)
        {
            case AbilityUpgradeType.Damage:
                _damage += GetDamageIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.FireFieldDuration:
                _fieldDuration += GetDurationIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.FireFieldRadius:
                _damageRadius += GetRadiusIncrease(upgrade.Value);
                break;
        }
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        _distanceSinceLastField = 0f;
        _hasLastCharacterPosition = false;
    }

    public override float GetStatFromIncrease() => _damage;

    public override float GetStatToIncrease(float upgradeMultiplier) =>
        GetDamageTo(upgradeMultiplier);

    public override AbilityUpgradePreview[] GetAcquirePreviews() =>
        new[]
        {
            new AbilityUpgradePreview(DamageStatName, _configuration.StartDamage),
            new AbilityUpgradePreview(DurationStatName, _configuration.FieldDuration, "s")
        };

    public override AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade)
    {
        return upgrade.Type switch
        {
            AbilityUpgradeType.FireFieldDuration =>
                new AbilityUpgradePreview(DurationStatName, _fieldDuration,
                    _fieldDuration + GetDurationIncrease(upgrade.Value), "s"),
            AbilityUpgradeType.FireFieldRadius =>
                new AbilityUpgradePreview(RadiusStatName, _damageRadius,
                    _damageRadius + GetRadiusIncrease(upgrade.Value), "m"),
            _ => new AbilityUpgradePreview(DamageStatName, _damage,
                GetDamageTo(upgrade.Value))
        };
    }

    public override float CalculateEstimatedDps()
    {
        if (_configuration == null || _configuration.Prefab == null)
            return 0f;

        float interval = Mathf.Max(0.05f, Mathf.Max(0.05f, _configuration.DamageTickInterval) /
            Mathf.Max(0.01f, _characterStats.RelicAttackSpeedMultiplier));
        float duration = Mathf.Max(0.1f, _fieldDuration * AbilityDurationMultiplier);
        float fieldsPerSecond = Mathf.Max(0f, _characterStats.MovementSpeed) /
                               Mathf.Max(0.1f, _distancePerField);
        // A single maintained field on the target; overlapping fields and extra targets are excluded.
        float uptime = Mathf.Clamp01(duration * fieldsPerSecond);
        return _damageCalculator.CalculateAverageDamage(_damage) * uptime / interval;
    }

    protected override void OnUse(CharacterFacade character)
    {
        if (character == null || _configuration.Prefab == null)
            return;

        TrackMovement(character);
        EnsureBurningSound(character);
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

        float duration = _fieldDuration * AbilityDurationMultiplier;
        damageArea.Initialize(_enemiesProvider, _damageRadius, _configuration.DamageHeight,
            _configuration.DamageTickInterval, duration, enemy => ApplyDamage(character, enemy),
            _characterStats, OnFieldExpired);
        _activeFields.Add(damageArea);
    }

    private void EnsureBurningSound(CharacterFacade character)
    {
        if (_activeFields.Count == 0 || _burningSound != null)
            return;

        Sounds.PlayLoop(SoundId.FireFieldLoop, character.transform);
        foreach (SoundLoopEmitter loop in character.GetComponentsInChildren<SoundLoopEmitter>())
        {
            if (loop.Sound != SoundId.FireFieldLoop || loop.transform.parent != character.transform ||
                !loop.isActiveAndEnabled)
                continue;

            _burningSound = loop;
            break;
        }
    }

    private void OnFieldExpired(FireFieldDamageArea field)
    {
        if (!_activeFields.Remove(field) || _activeFields.Count > 0 || _burningSound == null)
            return;

        _burningSound.Stop();
        _burningSound.gameObject.SetActive(false);
        Object.Destroy(_burningSound.gameObject);
        _burningSound = null;
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

    private void ApplyDamage(CharacterFacade character, CombatTarget enemy)
    {
        if (character == null || enemy == null || !enemy.CanBeAttacked || enemy.IsDead)
            return;

        Vector3 hitPosition = enemy.transform.position;
        CharacterDamageResult damageResult = _damageCalculator.Calculate(GetRolledDamage());
        int finalDamage = _relicManager.ModifyOutgoingDamage(damageResult.Damage, enemy);
        int appliedDamage = enemy.HealthSystem.GetDamage(finalDamage, damageResult.IsCritical);
        bool killedByHit = enemy.IsDead;

        if (appliedDamage <= 0)
            return;

        ApplyHitKnockback(character, enemy);

        float lifeStealPercent = Mathf.Max(0f, _characterStats.LifeSteal) * 0.01f;
        float healed = character.HealthSystem.IncreaseCurrentHealth(
            CharacterHealthRules.FromLegacyHealth(appliedDamage * lifeStealPercent));
        if (healed > 0f)
            _relicEventBus.PublishHeal(new RelicHealEvent(character, healed));

        enemy.EffectsSystem.DealDamage();
        _relicEventBus.PublishHit(new RelicHitEvent(character, enemy, finalDamage,
            damageResult.IsCritical, Id.ToString(), hitPosition, this, appliedDamage));

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

    private float GetDurationIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.DurationUpgradeIncrease, upgradeMultiplier);

    private float GetRadiusIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.RadiusUpgradeIncrease, upgradeMultiplier);
}
