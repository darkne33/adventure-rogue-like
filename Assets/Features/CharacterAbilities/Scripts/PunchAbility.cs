using System;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public sealed class PunchAbility : CharacterActiveAbility
{
    private const int PunchesPerSeries = 3;
    private const int PunchOverlapCapacity = 64;
    private const float MinimumCooldown = 0.05f;
    private const float MinimumPunchSpeed = 0.01f;
    private const float GoldenAngle = 137.5f;
    private const float DirectionEpsilon = 0.001f;

    private static readonly int PunchCollisionMask = LayerMask.GetMask("Default", "Enemy");
    private static readonly float[] TargetLateralOffsets = { -0.3f, 0.3f, 0f };
    private static readonly float[] TargetVerticalOffsets = { 0.15f, -0.1f, 0.25f };
    private const string DamageStatName = "Damage";
    private const string RadiusStatName = "Radius";
    private const string SpeedStatName = "Series Speed";
    private const string CooldownStatName = "Cooldown";
    private const string SeriesCountStatName = "Series";

    private static readonly AbilityUpgradeType[] PunchUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.PunchRadius,
        AbilityUpgradeType.ProjectileSpeed,
        AbilityUpgradeType.Cooldown,
        AbilityUpgradeType.AdditionalProjectiles
    };

    private static readonly AbilityUpgradeType[] PunchUpgradeTypesAtMinimumCooldown =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.PunchRadius,
        AbilityUpgradeType.ProjectileSpeed,
        AbilityUpgradeType.AdditionalProjectiles
    };

    private readonly IEnemiesProvider _enemiesProvider;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _characterStats;
    private readonly RelicEventBus _relicEventBus;
    private readonly RelicManager _relicManager;
    private readonly Collider[] _punchOverlapResults = new Collider[PunchOverlapCapacity];

    private PunchAbilityConfiguration _configuration;
    private int _damage;
    private float _radius;
    private float _punchSpeed;
    private float _additionalSeriesCount;
    private bool _isAttacking;
    private int _attackSequence;

    protected override bool StartCooldownImmediately => false;

    public override AbilityUpgradeType[] UpgradeTypes =>
        _configuration != null && Cooldown <= MinimumCooldown + Mathf.Epsilon
            ? PunchUpgradeTypesAtMinimumCooldown
            : PunchUpgradeTypes;

    public PunchAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
        CharacterStats characterStats, RelicEventBus relicEventBus, RelicManager relicManager)
    {
        _enemiesProvider = enemiesProvider;
        _damageCalculator = damageCalculator;
        _characterStats = characterStats;
        _relicEventBus = relicEventBus;
        _relicManager = relicManager;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        _attackSequence++;
        _isAttacking = false;
        base.Initialize(abilityConfig);

        _configuration = (PunchAbilityConfiguration)abilityConfig;
        Level = 0;
        Cooldown = Mathf.Max(MinimumCooldown, _configuration.Cooldown);
        CurrentCooldown = 0f;
        _damage = Mathf.Max(0, _configuration.StartDamage);
        _radius = Mathf.Max(0.1f, _configuration.Radius);
        _punchSpeed = Mathf.Max(MinimumPunchSpeed, _configuration.StartPunchSpeed);
        _additionalSeriesCount = 0f;

        StatName_1 = DamageStatName;
        StatName_2 = SeriesCountStatName;
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

    public override void OnUnequip(CharacterStats characterStats)
    {
        _attackSequence++;
        _isAttacking = false;
        _additionalSeriesCount = 0f;
        base.OnUnequip(characterStats);
    }

    public override float GetStatFromIncrease() =>
        _damage;

    public override float GetStatToIncrease(float upgradeMultiplier) =>
        GetDamageTo(upgradeMultiplier);

    public override AbilityUpgradePreview[] GetAcquirePreviews() =>
        new[]
        {
            new AbilityUpgradePreview(DamageStatName, _configuration.StartDamage),
            new AbilityUpgradePreview(SeriesCountStatName, 1f)
        };

    public override AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade)
    {
        if (upgrade.Type == AbilityUpgradeType.PunchRadius)
        {
            return new AbilityUpgradePreview(RadiusStatName, _radius,
                _radius + GetRadiusIncrease(upgrade.Value), "m");
        }

        return upgrade.Type switch
        {
            AbilityUpgradeType.ProjectileSpeed =>
                new AbilityUpgradePreview(SpeedStatName, _punchSpeed,
                    _punchSpeed + GetSpeedIncrease(upgrade.Value), "x"),
            AbilityUpgradeType.Cooldown =>
                new AbilityUpgradePreview(CooldownStatName, Cooldown,
                    GetCooldownTo(upgrade.Value), "s"),
            AbilityUpgradeType.AdditionalProjectiles =>
                new AbilityUpgradePreview(SeriesCountStatName, GetCurrentSeriesCount(),
                    GetCurrentSeriesCount() + GetAdditionalSeriesIncrease(upgrade.Value)),
            _ => new AbilityUpgradePreview(DamageStatName, _damage,
                GetDamageTo(upgrade.Value))
        };
    }

    protected override bool IsReady(CharacterFacade character) =>
        _isAttacking == false &&
        character != null &&
        _configuration != null &&
        _configuration.Prefab != null;

    protected override void OnUse(CharacterFacade character)
    {
        int seriesCount = CalculateSeriesCount();
        int attackSequence = ++_attackSequence;
        _isAttacking = true;
        LaunchPunchSeries(character, seriesCount, attackSequence).Forget();
    }

    private async UniTask LaunchPunchSeries(CharacterFacade character, int seriesCount, int attackSequence)
    {
        try
        {
            float punchDelay = Mathf.Max(0f, _configuration.PunchInterval) /
                               Mathf.Max(MinimumPunchSpeed, _punchSpeed);
            int totalPunchCount = Mathf.Max(1, seriesCount) * PunchesPerSeries;
            float idleAngleOffset = UnityEngine.Random.Range(0f, 360f);

            for (int index = 0; index < totalPunchCount; index++)
            {
                if (index > 0 && punchDelay > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(punchDelay),
                        cancellationToken: character.GetCancellationTokenOnDestroy());
                }

                if (attackSequence != _attackSequence)
                    return;

                int punchIndex = index % PunchesPerSeries;
                ExecutePunch(character, punchIndex, index, idleAngleOffset,
                    GetPunchDamage(punchIndex));
            }
        }
        finally
        {
            if (attackSequence == _attackSequence)
            {
                _isAttacking = false;
                StartCooldown();
            }
        }
    }

    private void ExecutePunch(CharacterFacade character, int punchIndex, int globalPunchIndex,
        float idleAngleOffset, int punchDamage)
    {
        EnemyFacade preferredEnemy = GetClosestEnemy(character);
        GetPunchPose(character, preferredEnemy, punchIndex, globalPunchIndex, idleAngleOffset,
            out Vector3 punchPosition, out Quaternion punchRotation);
        SpawnPunchEffect(punchPosition, punchRotation);

        if (TryGetCollisionTarget(punchPosition, preferredEnemy,
                out EnemyFacade hitEnemy, out Vector3 hitPosition) == false)
            return;

        ApplyDamage(character, hitEnemy, punchDamage, hitPosition);
    }

    private EnemyFacade GetClosestEnemy(CharacterFacade character) =>
        character == null
            ? null
            : _enemiesProvider.GetClosestEnemyByCharacter(character.transform, Mathf.Max(0.1f, _radius));

    private void GetPunchPose(CharacterFacade character, EnemyFacade preferredEnemy, int punchIndex,
        int globalPunchIndex, float idleAngleOffset, out Vector3 position, out Quaternion rotation)
    {
        if (preferredEnemy != null)
        {
            GetTargetPunchPose(character, preferredEnemy, punchIndex, out position, out rotation);
            return;
        }

        GetIdlePunchPose(character, globalPunchIndex, idleAngleOffset, out position, out rotation);
    }

    private void GetTargetPunchPose(CharacterFacade character, EnemyFacade enemy, int punchIndex,
        out Vector3 position, out Quaternion rotation)
    {
        Transform targetPoint = enemy.TargetToShootDamage != null
            ? enemy.TargetToShootDamage
            : enemy.transform;
        Collider targetCollider = GetEnemyCollider(enemy);
        Vector3 targetPosition = targetPoint.position;
        Vector3 towardCharacter = character.ProjectileSpawnPosition - targetPosition;
        towardCharacter.y = 0f;

        if (towardCharacter.sqrMagnitude <= DirectionEpsilon)
            towardCharacter = -character.transform.forward;

        towardCharacter.Normalize();

        if (targetCollider != null)
        {
            float probeDistance = targetCollider.bounds.extents.magnitude +
                                  Mathf.Max(1f, _configuration.ImpactRadius);
            Vector3 surfaceProbe = targetPosition + towardCharacter * probeDistance;
            position = targetCollider.ClosestPoint(surfaceProbe);
        }
        else
        {
            position = targetPosition + towardCharacter;
        }

        Vector3 tangent = Vector3.Cross(Vector3.up, towardCharacter).normalized;
        int offsetIndex = Mathf.Clamp(punchIndex, 0, PunchesPerSeries - 1);
        position += towardCharacter * _configuration.VisibleEffectOffset;
        position += tangent * TargetLateralOffsets[offsetIndex];
        position += Vector3.up * TargetVerticalOffsets[offsetIndex];

        Vector3 punchDirection = targetPosition - position;
        rotation = GetSafeRotation(punchDirection, -towardCharacter);
    }

    private void GetIdlePunchPose(CharacterFacade character, int globalPunchIndex,
        float idleAngleOffset, out Vector3 position, out Quaternion rotation)
    {
        float angle = (idleAngleOffset + globalPunchIndex * GoldenAngle) * Mathf.Deg2Rad;
        Vector3 radialDirection = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        float radiusProgress = Mathf.Repeat((globalPunchIndex + 1) * 0.618034f, 1f);
        float distance = Mathf.Lerp(_configuration.IdleEffectMinDistance,
            _configuration.IdleEffectMaxDistance, radiusProgress);
        float heightOffset = (globalPunchIndex % PunchesPerSeries - 1) * 0.15f;
        Vector3 center = character.ProjectileSpawnPosition;

        position = center + radialDirection * distance + Vector3.up * heightOffset;
        rotation = GetSafeRotation(center - position, -radialDirection);
    }

    private bool TryGetCollisionTarget(Vector3 punchPosition, EnemyFacade preferredEnemy,
        out EnemyFacade hitEnemy, out Vector3 hitPosition)
    {
        hitEnemy = null;
        hitPosition = punchPosition;
        float closestSqrDistance = float.PositiveInfinity;
        bool preferredEnemyFound = false;
        int hitCount = Physics.OverlapSphereNonAlloc(punchPosition,
            Mathf.Max(0.01f, _configuration.ImpactRadius), _punchOverlapResults,
            PunchCollisionMask, QueryTriggerInteraction.Ignore);

        for (int index = 0; index < hitCount; index++)
        {
            Collider hitCollider = _punchOverlapResults[index];
            _punchOverlapResults[index] = null;
            if (hitCollider == null)
                continue;

            EnemyFacade enemy = hitCollider.GetComponentInParent<EnemyFacade>();
            if (enemy == null || enemy.IsDead || enemy.gameObject.activeInHierarchy == false)
                continue;

            Vector3 closestPoint = hitCollider.ClosestPoint(punchPosition);
            if (enemy == preferredEnemy)
            {
                hitEnemy = enemy;
                hitPosition = closestPoint;
                preferredEnemyFound = true;
                continue;
            }

            if (preferredEnemyFound)
                continue;

            float sqrDistance = (closestPoint - punchPosition).sqrMagnitude;
            if (sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            hitEnemy = enemy;
            hitPosition = closestPoint;
        }

        return hitEnemy != null;
    }

    private static Collider GetEnemyCollider(EnemyFacade enemy)
    {
        if (enemy == null)
            return null;

        Collider collider = enemy.GetComponent<Collider>();
        if (collider != null && collider.enabled)
            return collider;

        return enemy.GetComponentInChildren<Collider>();
    }

    private static Quaternion GetSafeRotation(Vector3 direction, Vector3 fallbackDirection)
    {
        if (direction.sqrMagnitude <= DirectionEpsilon)
            direction = fallbackDirection;
        if (direction.sqrMagnitude <= DirectionEpsilon)
            direction = Vector3.forward;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void SpawnPunchEffect(Vector3 position, Quaternion rotation)
    {
        if (_configuration.Prefab == null)
            return;

        GameObject effect = UnityEngine.Object.Instantiate(_configuration.Prefab,
            position, rotation);
        float duration = Mathf.Max(0.01f, _configuration.EffectDuration * AbilityDurationMultiplier);
        UnityEngine.Object.Destroy(effect, duration);
    }

    private void ApplyDamage(CharacterFacade character, EnemyFacade enemy, int baseDamage, Vector3 hitPosition)
    {
        if (character == null || enemy == null || enemy.IsDead || baseDamage <= 0)
            return;

        CharacterDamageResult damageResult = _damageCalculator.Calculate(GetRolledDamage(baseDamage));
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

    private int GetPunchDamage(int punchIndex)
    {
        int totalDamage = Mathf.Max(0, _damage);
        int damagePerPunch = totalDamage / PunchesPerSeries;
        int remainder = totalDamage % PunchesPerSeries;
        return damagePerPunch + (punchIndex < remainder ? 1 : 0);
    }

    private int GetRolledDamage(int baseDamage)
    {
        float variation = Mathf.Max(0f, _configuration.DamageVariationPercent) * 0.01f;
        float multiplier = UnityEngine.Random.Range(1f - variation, 1f + variation);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }

    private int CalculateSeriesCount()
    {
        float seriesBonus = Mathf.Max(0f, _characterStats.ProjectileCount) +
                            Mathf.Max(0f, _additionalSeriesCount);
        int seriesCount = 1 + Mathf.FloorToInt(seriesBonus);
        float fractionalSeries = seriesBonus - Mathf.Floor(seriesBonus);

        if (UnityEngine.Random.value < fractionalSeries)
            seriesCount++;

        return Mathf.Max(1, seriesCount);
    }

    private float GetCurrentSeriesCount() =>
        1f + Mathf.Max(0f, _characterStats.ProjectileCount) + Mathf.Max(0f, _additionalSeriesCount);

    private void ApplyUpgradeEffect(AbilityUpgradeEffect upgrade)
    {
        if (upgrade.Type == AbilityUpgradeType.PunchRadius)
        {
            _radius += GetRadiusIncrease(upgrade.Value);
            return;
        }

        switch (upgrade.Type)
        {
            case AbilityUpgradeType.Damage:
                _damage += GetDamageIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.ProjectileSpeed:
                _punchSpeed += GetSpeedIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.Cooldown:
                Cooldown = GetCooldownTo(upgrade.Value);
                break;
            case AbilityUpgradeType.AdditionalProjectiles:
                _additionalSeriesCount += GetAdditionalSeriesIncrease(upgrade.Value);
                break;
        }
    }

    private int GetDamageIncrease(float upgradeMultiplier) =>
        Mathf.Max(0,
            Mathf.RoundToInt(GetUpgradeValue(_configuration.DamageUpgradeIncrease, upgradeMultiplier)));

    private int GetDamageTo(float upgradeMultiplier) =>
        _damage + GetDamageIncrease(upgradeMultiplier);

    private float GetRadiusIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.RadiusUpgradeIncrease, upgradeMultiplier);

    private float GetSpeedIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(_configuration.SpeedUpgradeIncrease, upgradeMultiplier);

    private float GetCooldownTo(float upgradeMultiplier) =>
        Mathf.Max(MinimumCooldown,
            Cooldown - GetUpgradeValue(_configuration.CooldownUpgradeReduction, upgradeMultiplier));

    private static float GetAdditionalSeriesIncrease(float upgradeMultiplier) =>
        Mathf.Max(0f, upgradeMultiplier);

    private void RefreshStats()
    {
        Stat_1 = _damage;
        Stat_2 = GetCurrentSeriesCount();
    }
}
