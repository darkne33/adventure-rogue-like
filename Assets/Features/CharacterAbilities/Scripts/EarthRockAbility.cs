using System.Collections.Generic;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public sealed class EarthRockAbility : CharacterActiveAbility
{
    private const string DamageStatName = "Damage";
    private const string RadiusStatName = "Radius";
    private const string RotationSpeedStatName = "Rotation Speed";
    private const string StoneCountStatName = "Stones";

    private static readonly AbilityUpgradeType[] EarthRockUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.EarthRockRadius,
        AbilityUpgradeType.EarthRockRotationSpeed,
        AbilityUpgradeType.EarthRockStoneCount
    };

    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _characterStats;
    private readonly RelicEventBus _relicEventBus;
    private readonly RelicManager _relicManager;
    private readonly List<OrbitSlot> _slots = new();

    private EarthRockAbilityConfiguration _configuration;
    private CharacterFacade _owner;
    private int _damage;
    private int _stoneCount;
    private float _orbitRadius;
    private float _rotationSpeed;
    private float _orbitAngle;

    public override AbilityUpgradeType[] UpgradeTypes => EarthRockUpgradeTypes;

    public EarthRockAbility(CharacterDamageCalculator damageCalculator, CharacterStats characterStats,
        RelicEventBus relicEventBus, RelicManager relicManager)
    {
        _damageCalculator = damageCalculator;
        _characterStats = characterStats;
        _relicEventBus = relicEventBus;
        _relicManager = relicManager;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        CleanupOrbit();
        base.Initialize(abilityConfig);

        _configuration = (EarthRockAbilityConfiguration)abilityConfig;
        Level = 0;
        Cooldown = 0f;
        CurrentCooldown = 0f;
        _damage = _configuration.StartDamage;
        _stoneCount = _configuration.StartStoneCount;
        _orbitRadius = _configuration.OrbitRadius;
        _rotationSpeed = _configuration.RotationSpeed;
        _orbitAngle = 0f;

        StatName_1 = DamageStatName;
        StatName_2 = StoneCountStatName;
        RefreshStats();
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);

        ApplyUpgradeEffect(CurrentPrimaryUpgrade);
        if (CurrentSecondaryUpgrade.HasValue)
            ApplyUpgradeEffect(CurrentSecondaryUpgrade.Value);

        RefreshStats();
    }

    private void ApplyUpgradeEffect(AbilityUpgradeEffect upgrade)
    {
        switch (upgrade.Type)
        {
            case AbilityUpgradeType.Damage:
                _damage += GetDamageIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.EarthRockRadius:
                _orbitRadius += GetRadiusIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.EarthRockRotationSpeed:
                _rotationSpeed += GetRotationSpeedIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.EarthRockStoneCount:
                _stoneCount += GetStoneCountIncrease(upgrade.Value);
                break;
        }
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        CleanupOrbit();
        base.OnUnequip(characterStats);
    }

    public override float GetStatFromIncrease() =>
        _damage;

    public override float GetStatToIncrease(float upgradeMultiplier) =>
        GetDamageTo(upgradeMultiplier);

    public override AbilityUpgradePreview[] GetAcquirePreviews() =>
        new[]
        {
            new AbilityUpgradePreview(StoneCountStatName, _configuration.StartStoneCount),
            new AbilityUpgradePreview(DamageStatName, _configuration.StartDamage)
        };

    public override AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade)
    {
        return upgrade.Type switch
        {
            AbilityUpgradeType.EarthRockRadius =>
                new AbilityUpgradePreview(RadiusStatName, _orbitRadius,
                    _orbitRadius + GetRadiusIncrease(upgrade.Value), "m"),
            AbilityUpgradeType.EarthRockRotationSpeed =>
                new AbilityUpgradePreview(RotationSpeedStatName, _rotationSpeed,
                    _rotationSpeed + GetRotationSpeedIncrease(upgrade.Value), "°/s"),
            AbilityUpgradeType.EarthRockStoneCount =>
                new AbilityUpgradePreview(StoneCountStatName, _stoneCount,
                    _stoneCount + GetStoneCountIncrease(upgrade.Value)),
            _ => new AbilityUpgradePreview(DamageStatName, _damage,
                GetDamageTo(upgrade.Value))
        };
    }

    protected override void OnUse(CharacterFacade character)
    {
        if (character == null || _configuration.Prefab == null)
            return;

        if (_owner != character)
        {
            CleanupOrbit();
            _owner = character;
        }

        EnsureSlotCount();
        _orbitAngle = Mathf.Repeat(_orbitAngle + _rotationSpeed * Time.deltaTime, 360f);
        UpdateRespawns(character);
        UpdateStoneTransforms(character);
    }

    private void EnsureSlotCount()
    {
        int targetCount = Mathf.Max(1, _stoneCount);

        while (_slots.Count < targetCount)
            _slots.Add(new OrbitSlot());

        while (_slots.Count > targetCount)
        {
            int lastIndex = _slots.Count - 1;
            OrbitSlot slot = _slots[lastIndex];
            if (slot.Stone != null)
                Object.Destroy(slot.Stone);

            _slots.RemoveAt(lastIndex);
        }
    }

    private void UpdateRespawns(CharacterFacade character)
    {
        for (int index = 0; index < _slots.Count; index++)
        {
            OrbitSlot slot = _slots[index];
            if (slot.Stone != null)
                continue;

            slot.RespawnRemaining = Mathf.Max(0f, slot.RespawnRemaining - Time.deltaTime);
            if (slot.RespawnRemaining <= 0f)
                SpawnStone(character, slot, index);
        }
    }

    private void SpawnStone(CharacterFacade character, OrbitSlot slot, int slotIndex)
    {
        GetStoneTransform(character, slotIndex, out Vector3 position, out Quaternion rotation);
        GameObject stone = Object.Instantiate(_configuration.Prefab, position, rotation, character.transform);
        PlayerCollisionDetector collisionDetector = stone.GetComponent<PlayerCollisionDetector>();

        if (collisionDetector == null)
        {
            Debug.LogError($"{_configuration.Prefab.name} is missing {nameof(PlayerCollisionDetector)}.");
            Object.Destroy(stone);
            slot.RespawnRemaining = _configuration.RespawnDelay;
            return;
        }

        slot.Stone = stone;
        slot.RespawnRemaining = 0f;
        collisionDetector.Initialize(character.transform);
        collisionDetector.OnHit = enemy =>
            HandleStoneHit(character, slot, stone, collisionDetector, enemy);
    }

    private void UpdateStoneTransforms(CharacterFacade character)
    {
        for (int index = 0; index < _slots.Count; index++)
        {
            GameObject stone = _slots[index].Stone;
            if (stone == null)
                continue;

            GetStoneTransform(character, index, out Vector3 position, out Quaternion rotation);
            stone.transform.SetPositionAndRotation(position, rotation);
        }
    }

    private void GetStoneTransform(CharacterFacade character, int slotIndex, out Vector3 position,
        out Quaternion rotation)
    {
        float slotAngle = _orbitAngle + 360f * slotIndex / Mathf.Max(1, _slots.Count);
        float angleRadians = slotAngle * Mathf.Deg2Rad;
        Vector3 radialDirection = new(Mathf.Cos(angleRadians), 0f, Mathf.Sin(angleRadians));
        Vector3 tangentDirection = new(-Mathf.Sin(angleRadians), 0f, Mathf.Cos(angleRadians));
        Vector3 orbitCenter = character.transform.position + Vector3.up * _configuration.OrbitHeight;

        position = orbitCenter + radialDirection * _orbitRadius;
        rotation = Quaternion.LookRotation(tangentDirection, Vector3.up);
    }

    private void HandleStoneHit(CharacterFacade character, OrbitSlot slot, GameObject stone,
        PlayerCollisionDetector collisionDetector, EnemyFacade enemy)
    {
        if (slot.Stone != stone)
            return;

        if (enemy == null || enemy.IsDead)
        {
            collisionDetector.ResetHit();
            return;
        }

        SpawnEarthPoof(stone.transform.position);
        slot.Stone = null;
        slot.RespawnRemaining = Mathf.Max(0f, _configuration.RespawnDelay);
        ApplyDamage(character, enemy);
        Object.Destroy(stone);
    }

    private void SpawnEarthPoof(Vector3 position)
    {
        if (_configuration.EarthPoofPrefab == null)
            return;

        Object.Instantiate(_configuration.EarthPoofPrefab, position, Quaternion.identity);
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
        _relicEventBus.PublishHit(new RelicHitEvent(character, enemy, finalDamage,
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
        _damage + GetDamageIncrease(upgradeMultiplier);

    private float GetRadiusIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.RadiusUpgradeIncrease, upgradeMultiplier);

    private float GetRotationSpeedIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.RotationSpeedUpgradeIncrease, upgradeMultiplier);

    private int GetStoneCountIncrease(float upgradeMultiplier) =>
        Mathf.Max(1,
            Mathf.RoundToInt(GetUpgradeValue(_configuration.StoneCountUpgradeIncrease, upgradeMultiplier)));

    private void RefreshStats()
    {
        Stat_1 = _damage;
        Stat_2 = _stoneCount;
    }

    private void CleanupOrbit()
    {
        foreach (OrbitSlot slot in _slots)
        {
            if (slot.Stone != null)
                Object.Destroy(slot.Stone);
        }

        _slots.Clear();
        _owner = null;
        _orbitAngle = 0f;
    }

    private sealed class OrbitSlot
    {
        public GameObject Stone;
        public float RespawnRemaining;
    }
}
