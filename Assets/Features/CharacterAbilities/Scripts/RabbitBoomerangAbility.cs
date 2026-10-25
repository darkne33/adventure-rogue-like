using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using Features.Sounds;
using UnityEngine;

public sealed class RabbitBoomerangAbility : SingleShootAbility
{
    private const string DamageStatName = "Damage";
    private const string SpeedStatName = "Speed";
    private const string SizeStatName = "Size";

    private static readonly AbilityUpgradeType[] BoomerangUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.ProjectileSpeed,
        AbilityUpgradeType.ProjectileSize,
        AbilityUpgradeType.AdditionalProjectiles
    };

    private readonly IRelicProjectileContext _projectileContext;
    private CancellationTokenSource _flightLifetime;
    private float _size;
    private int _launchRoomSequence;
    private bool _useAlternateShotSound;

    private RabbitBoomerangAbilityConfiguration BoomerangConfig =>
        (RabbitBoomerangAbilityConfiguration)AbilityConfig;

    protected override int BaseProjectileCount => Mathf.Max(1, BoomerangConfig.StartProjectileCount);
    public override AbilityUpgradeType[] UpgradeTypes => BoomerangUpgradeTypes;

    public RabbitBoomerangAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
        CharacterStats characterStats, RelicEventBus relicEventBus, RelicManager relicManager)
        : base(enemiesProvider, damageCalculator, characterStats, relicEventBus, relicManager)
    {
        _projectileContext = relicManager.ProjectileContext;
    }

    public override float GetStatFromIncrease() => Level <= 0 ? 0f : Damage;

    public override float GetStatToIncrease(float upgradeMultiplier) => GetDamageTo(upgradeMultiplier);

    public override float CalculateEstimatedDps() => base.CalculateEstimatedDps() * 2f;

    public override AbilityUpgradePreview[] GetAcquirePreviews() => new[]
    {
        new AbilityUpgradePreview(DamageStatName, AbilityConfig.StartDamage),
        new AbilityUpgradePreview("Projectiles", BaseProjectileCount)
    };

    public override AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade) =>
        upgrade.Type switch
        {
            AbilityUpgradeType.AdditionalProjectiles => GetAdditionalProjectileUpgradePreview(upgrade),
            AbilityUpgradeType.ProjectileSize => new AbilityUpgradePreview(SizeStatName, _size * 100f,
                (_size + GetSizeIncrease(upgrade.Value)) * 100f, "%"),
            AbilityUpgradeType.ProjectileSpeed => new AbilityUpgradePreview(SpeedStatName, ProjectileSpeed,
                ProjectileSpeed + GetSpeedIncrease(upgrade.Value)),
            _ => new AbilityUpgradePreview(DamageStatName, Damage, GetDamageTo(upgrade.Value))
        };

    public override void OnUnequip(CharacterStats characterStats)
    {
        CancelFlights();
        base.OnUnequip(characterStats);
    }

    public override void RestartCooldown()
    {
        CancelFlights();
        base.RestartCooldown();
    }

    protected override void OnShootableInitialized()
    {
        CancelFlights();
        Level = 0;
        Damage = AbilityConfig.StartDamage;
        _size = 1f;
        _useAlternateShotSound = false;
        RefreshStats();
    }

    protected override void OnShootableEquipped(CharacterStats characterStats)
    {
        ApplyUpgradeEffect(CurrentPrimaryUpgrade);
        if (CurrentSecondaryUpgrade.HasValue)
            ApplyUpgradeEffect(CurrentSecondaryUpgrade.Value);
        RefreshStats();
    }

    protected override void OnUse(CharacterFacade character)
    {
        _launchRoomSequence = _projectileContext.RoomSequence;
        base.OnUse(character);
    }

    protected override bool CanContinueProjectileLaunch(CharacterFacade character) =>
        IsAcquired && character != null && character.isActiveAndEnabled &&
        !character.HealthSystem.IsDead && !character.IsTransitionPaused && !character.IsCinematicPaused &&
        _launchRoomSequence == _projectileContext.RoomSequence;

    protected override CombatTarget FindProjectileTarget(CharacterFacade character)
    {
        Vector3 origin = character.ProjectileSpawnPosition;
        float range = Mathf.Max(0.1f, BoomerangConfig.TargetingRange);
        float closestDistance = range * range;
        CombatTarget closest = null;
        foreach (CombatTarget candidate in EnemiesProvider.ActiveEnemies)
        {
            if (candidate == null || !candidate.CanBeAttacked || candidate.IsDead ||
                !candidate.gameObject.activeInHierarchy)
                continue;

            Vector3 offset = GetEnemyTargetPosition(candidate) - origin;
            if (offset.sqrMagnitude > closestDistance || !HasLineOfSight(origin, offset))
                continue;

            closestDistance = offset.sqrMagnitude;
            closest = candidate;
        }
        return closest;
    }

    private static bool HasLineOfSight(Vector3 origin, Vector3 offset)
    {
        if (offset.sqrMagnitude <= 0.0001f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(origin, offset.normalized, offset.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            Collider collider = hit.collider;
            if (collider.GetComponentInParent<CombatTarget>() == null &&
                collider.GetComponentInParent<CharacterFacade>() == null &&
                collider.GetComponentInParent<PlayerCollisionDetector>() == null &&
                collider.GetComponentInParent<EnemyProjectile>() == null)
                return false;
        }
        return true;
    }

    protected override void OnProjectileCreated(CharacterFacade character, GameObject shootObj,
        PlayerCollisionDetector collisionDetector, CombatTarget targetEnemy, Vector3 spawnPosition,
        Vector3 targetPosition, Vector3 shootDirection, int projectileDamage)
    {
        if (!shootObj.TryGetComponent(out SphereCollider hitbox))
        {
            Debug.LogError("Rabbitarang requires a SphereCollider on its prepared projectile prefab.", shootObj);
            Object.Destroy(shootObj);
            return;
        }

        _flightLifetime ??= new CancellationTokenSource();
        shootObj.transform.localScale *= _size;
        Sounds.Play(_useAlternateShotSound ? SoundId.BoomerangShotAlternate : SoundId.BoomerangShot);
        _useAlternateShotSound = !_useAlternateShotSound;
        var flight = new RabbitBoomerangFlight(shootObj, hitbox, collisionDetector, character,
            _projectileContext, BoomerangConfig, ProjectileSpeed, shootDirection,
            (target, distance) => ApplyDamage(character, target, projectileDamage, distance, collisionDetector),
            () => Sounds.Play(SoundId.BoomerangImpact));
        flight.Run(_flightLifetime.Token).Forget();
    }

    private void ApplyUpgradeEffect(AbilityUpgradeEffect upgrade)
    {
        switch (upgrade.Type)
        {
            case AbilityUpgradeType.Damage:
                IncreaseDamage(GetDamageIncrease(upgrade.Value));
                break;
            case AbilityUpgradeType.ProjectileSpeed:
                IncreaseProjectileSpeed(GetSpeedIncrease(upgrade.Value));
                break;
            case AbilityUpgradeType.ProjectileSize:
                _size += GetSizeIncrease(upgrade.Value);
                break;
        }
    }

    private void RefreshStats()
    {
        StatName_1 = DamageStatName;
        Stat_1 = Damage;
        StatName_2 = SizeStatName;
        Stat_2 = _size * 100f;
    }

    private void CancelFlights()
    {
        _flightLifetime?.Cancel();
        _flightLifetime?.Dispose();
        _flightLifetime = null;
    }

    private float GetDamageIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(BoomerangConfig.DamageUpgradeIncrease, upgradeMultiplier);

    private float GetDamageTo(float upgradeMultiplier) =>
        (Damage <= 0 ? AbilityConfig.StartDamage : Damage) + GetDamageIncrease(upgradeMultiplier);

    private float GetSpeedIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(BoomerangConfig.SpeedUpgradeIncrease, upgradeMultiplier);

    private float GetSizeIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(BoomerangConfig.SizeUpgradeIncrease, upgradeMultiplier);
}
