using DG.Tweening;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public class FireballAbility : SingleShootAbility
{
    private const string DamageStatName = "Damage";
    private const string SpeedStatName = "Speed";
    private const string CooldownStatName = "Cooldown";

    private static readonly AbilityUpgradeType[] FireballUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.ProjectileSpeed,
        AbilityUpgradeType.Cooldown,
        AbilityUpgradeType.AdditionalProjectiles
    };

    private static readonly AbilityUpgradeType[] FireballUpgradeTypesAtMinimumCooldown =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.ProjectileSpeed,
        AbilityUpgradeType.AdditionalProjectiles
    };

    private float _travelDistance;

    private FireballAbilityConfiguration FireballConfig => (FireballAbilityConfiguration)AbilityConfig;
    public override AbilityUpgradeType[] UpgradeTypes =>
        AbilityConfig != null && Cooldown <= MinimumCooldown + Mathf.Epsilon
            ? FireballUpgradeTypesAtMinimumCooldown
            : FireballUpgradeTypes;

    public FireballAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
        CharacterStats characterStats, RelicEventBus relicEventBus, RelicManager relicManager)
        : base(enemiesProvider, damageCalculator, characterStats, relicEventBus, relicManager)
    {
    }

    public override float GetStatFromIncrease() =>
        Stat_1;

    public override float GetStatToIncrease(float upgradeMultiplier) =>
        GetDamageTo(upgradeMultiplier);

    public override AbilityUpgradePreview[] GetAcquirePreviews() =>
        new[]
        {
            new AbilityUpgradePreview(DamageStatName, AbilityConfig.StartDamage),
            new AbilityUpgradePreview(CooldownStatName, AbilityConfig.Cooldown, "s")
        };

    public override AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade)
    {
        return upgrade.Type switch
        {
            AbilityUpgradeType.AdditionalProjectiles =>
                GetAdditionalProjectileUpgradePreview(upgrade),
            AbilityUpgradeType.ProjectileSpeed =>
                new AbilityUpgradePreview(SpeedStatName, ProjectileSpeed,
                    ProjectileSpeed + GetSpeedIncrease(upgrade.Value)),
            AbilityUpgradeType.Cooldown =>
                new AbilityUpgradePreview(CooldownStatName, Cooldown,
                    GetCooldownTo(upgrade.Value), "s"),
            _ => new AbilityUpgradePreview(DamageStatName, Damage,
                GetDamageTo(upgrade.Value))
        };
    }

    protected override void OnShootableInitialized()
    {
        StatName_1 = DamageStatName;
        _travelDistance = FireballConfig.TravelDistance;
    }

    protected override void OnShootableEquipped(CharacterStats characterStats)
    {
        if (Damage <= 0)
            Damage = AbilityConfig.StartDamage;

        ApplyUpgradeEffect(CurrentPrimaryUpgrade);
        if (CurrentSecondaryUpgrade.HasValue)
            ApplyUpgradeEffect(CurrentSecondaryUpgrade.Value);

        Stat_1 = Damage;
    }

    private void ApplyUpgradeEffect(AbilityUpgradeEffect upgrade)
    {
        switch (upgrade.Type)
        {
            case AbilityUpgradeType.ProjectileSpeed:
                IncreaseProjectileSpeed(GetSpeedIncrease(upgrade.Value));
                break;
            case AbilityUpgradeType.Damage:
                IncreaseDamage(GetDamageIncrease(upgrade.Value));
                break;
            case AbilityUpgradeType.Cooldown:
                ReduceCooldown(GetCooldownReduction(upgrade.Value));
                break;
        }
    }

    private float GetDamageIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(FireballConfig.DamageUpgradeIncrease, upgradeMultiplier);

    private float GetDamageTo(float upgradeMultiplier) =>
        (Damage <= 0 ? AbilityConfig.StartDamage : Damage) + GetDamageIncrease(upgradeMultiplier);

    private float GetSpeedIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(FireballConfig.SpeedUpgradeIncrease, upgradeMultiplier);

    private float GetCooldownReduction(float upgradeMultiplier) =>
        GetUpgradeValue(FireballConfig.CooldownUpgradeReduction, upgradeMultiplier);

    private float GetCooldownTo(float upgradeMultiplier) =>
        Mathf.Max(MinimumCooldown, Cooldown - GetCooldownReduction(upgradeMultiplier));

    protected override void OnProjectileCreated(CharacterFacade character, GameObject shootObj,
        PlayerCollisionDetector collisionDetector, EnemyFacade targetEnemy, Vector3 spawnPosition,
        Vector3 shootDirection)
    {
        Vector3 endPosition = spawnPosition + shootDirection * _travelDistance;
        MoveProjectile(shootObj, endPosition).OnComplete(() => DestroyShoot(shootObj));
        collisionDetector.OnHit = enemyFacade => DamageDeal(character, shootObj, enemyFacade);
    }

    private void DamageDeal(CharacterFacade character, GameObject shootObj, EnemyFacade enemyFacade)
    {
        if (enemyFacade == null || enemyFacade.HealthSystem.IsDead)
        {
            DestroyShoot(shootObj);
            return;
        }

        ApplyDamage(character, enemyFacade);
        DestroyShoot(shootObj);
    }
}
