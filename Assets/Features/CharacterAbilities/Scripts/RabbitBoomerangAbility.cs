using System.Collections.Generic;
using DG.Tweening;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public class RabbitBoomerangAbility : SingleShootAbility
{
    private const string BoomerangStatName = "Targets";
    private const string SpeedStatName = "Speed";
    private const string CooldownStatName = "Cooldown";
    private const string BounceRadiusStatName = "Bounce Radius";
    private const string DamageStatName = "Damage";

    private static readonly AbilityUpgradeType[] BoomerangUpgradeTypes =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.ProjectileSpeed,
        AbilityUpgradeType.Cooldown,
        AbilityUpgradeType.BounceRadius,
        AbilityUpgradeType.Targets,
        AbilityUpgradeType.AdditionalProjectiles
    };

    private static readonly AbilityUpgradeType[] BoomerangUpgradeTypesAtMinimumCooldown =
    {
        AbilityUpgradeType.Damage,
        AbilityUpgradeType.ProjectileSpeed,
        AbilityUpgradeType.BounceRadius,
        AbilityUpgradeType.Targets,
        AbilityUpgradeType.AdditionalProjectiles
    };

    private int _bonusTargets;
    private float _bounceRadius;

    private RabbitBoomerangAbilityConfiguration BoomerangConfig =>
        (RabbitBoomerangAbilityConfiguration)AbilityConfig;
    public override AbilityUpgradeType[] UpgradeTypes =>
        AbilityConfig != null && Cooldown <= MinimumCooldown + Mathf.Epsilon
            ? BoomerangUpgradeTypesAtMinimumCooldown
            : BoomerangUpgradeTypes;

    public RabbitBoomerangAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
        CharacterStats characterStats, RelicEventBus relicEventBus, RelicManager relicManager)
        : base(enemiesProvider, damageCalculator, characterStats, relicEventBus, relicManager)
    {
    }

    public override float GetStatFromIncrease() =>
        Level <= 0 ? 0 : GetBoomerangMaxHitCount();

    public override float GetStatToIncrease(float upgradeMultiplier) =>
        GetTargetsTo(upgradeMultiplier);

    public override AbilityUpgradePreview[] GetAcquirePreviews() =>
        new[]
        {
            new AbilityUpgradePreview(BoomerangStatName, BoomerangConfig.StartTargets),
            new AbilityUpgradePreview(DamageStatName, AbilityConfig.StartDamage)
        };

    public override AbilityUpgradePreview GetUpgradePreview(AbilityUpgradeEffect upgrade)
    {
        return upgrade.Type switch
        {
            AbilityUpgradeType.AdditionalProjectiles =>
                GetAdditionalProjectileUpgradePreview(upgrade),
            AbilityUpgradeType.BounceRadius =>
                new AbilityUpgradePreview(BounceRadiusStatName, _bounceRadius,
                    _bounceRadius + GetBounceRadiusIncrease(upgrade.Value), "m"),
            AbilityUpgradeType.Targets =>
                new AbilityUpgradePreview(BoomerangStatName, GetTargetsFrom(),
                    GetTargetsTo(upgrade.Value)),
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
        Level = 0;
        _bonusTargets = 0;
        _bounceRadius = BoomerangConfig.BounceRadius;
        StatName_1 = BoomerangStatName;
        Stat_1 = BoomerangConfig.StartTargets;
    }

    protected override void OnShootableEquipped(CharacterStats characterStats)
    {
        if (Damage <= 0)
            Damage = AbilityConfig.StartDamage;

        ApplyUpgradeEffect(CurrentPrimaryUpgrade);
        if (CurrentSecondaryUpgrade.HasValue)
            ApplyUpgradeEffect(CurrentSecondaryUpgrade.Value);

        Stat_1 = GetBoomerangMaxHitCount();
    }

    private void ApplyUpgradeEffect(AbilityUpgradeEffect upgrade)
    {
        switch (upgrade.Type)
        {
            case AbilityUpgradeType.BounceRadius:
                _bounceRadius += GetBounceRadiusIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.ProjectileSpeed:
                IncreaseProjectileSpeed(GetSpeedIncrease(upgrade.Value));
                break;
            case AbilityUpgradeType.Cooldown:
                ReduceCooldown(GetCooldownReduction(upgrade.Value));
                break;
            case AbilityUpgradeType.Targets:
                _bonusTargets += GetTargetIncrease(upgrade.Value);
                break;
            case AbilityUpgradeType.Damage:
                IncreaseDamage(GetDamageIncrease(upgrade.Value));
                break;
        }
    }

    protected override void OnProjectileCreated(CharacterFacade character, GameObject shootObj,
        PlayerCollisionDetector collisionDetector, EnemyFacade targetEnemy, Vector3 spawnPosition,
        Vector3 shootDirection, int projectileDamage)
    {
        HashSet<EnemyFacade> hitEnemies = new();
        collisionDetector.OnHit = enemyFacade =>
            DamageDeal(character, shootObj, enemyFacade, collisionDetector, hitEnemies, projectileDamage);
        MoveBoomerangToEnemy(character, shootObj, collisionDetector, hitEnemies, targetEnemy, projectileDamage);
    }

    private void DamageDeal(CharacterFacade character, GameObject shootObj, EnemyFacade enemyFacade,
        PlayerCollisionDetector collisionDetector, HashSet<EnemyFacade> hitEnemies, int projectileDamage)
    {
        if (shootObj == null)
            return;

        if (enemyFacade == null)
        {
            DestroyShoot(shootObj);
            return;
        }

        if (hitEnemies.Count >= GetBoomerangMaxHitCount())
        {
            DestroyShoot(shootObj);
            return;
        }

        if (hitEnemies.Contains(enemyFacade))
        {
            collisionDetector.ResetHit();
            return;
        }

        if (enemyFacade.HealthSystem.IsDead)
        {
            DestroyShoot(shootObj);
            return;
        }

        hitEnemies.Add(enemyFacade);
        ApplyDamage(character, enemyFacade, projectileDamage);

        if (hitEnemies.Count >= GetBoomerangMaxHitCount())
        {
            DestroyShoot(shootObj);
            return;
        }

        TryBounceBoomerang(character, shootObj, collisionDetector, hitEnemies, projectileDamage);
    }

    private void TryBounceBoomerang(CharacterFacade character, GameObject shootObj,
        PlayerCollisionDetector collisionDetector, HashSet<EnemyFacade> hitEnemies, int projectileDamage)
    {
        EnemyFacade nextEnemy = FindNextBoomerangTarget(shootObj.transform.position, hitEnemies);
        if (nextEnemy == null)
        {
            DestroyShoot(shootObj);
            return;
        }

        MoveBoomerangToEnemy(character, shootObj, collisionDetector, hitEnemies, nextEnemy, projectileDamage);
    }

    private EnemyFacade FindNextBoomerangTarget(Vector3 projectilePosition, HashSet<EnemyFacade> hitEnemies)
    {
        EnemyFacade closestEnemy = null;
        float closestSqrDistance = float.MaxValue;
        float maxSqrDistance = _bounceRadius * _bounceRadius;

        foreach (EnemyFacade enemy in EnemiesProvider.ActiveEnemies)
        {
            if (enemy == null || hitEnemies.Contains(enemy) || enemy.HealthSystem.IsDead)
                continue;

            float sqrDistance = (GetEnemyTargetPosition(enemy) - projectilePosition).sqrMagnitude;
            if (sqrDistance > maxSqrDistance || sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            closestEnemy = enemy;
        }

        return closestEnemy;
    }

    private void MoveBoomerangToEnemy(CharacterFacade character, GameObject shootObj,
        PlayerCollisionDetector collisionDetector, HashSet<EnemyFacade> hitEnemies, EnemyFacade nextEnemy,
        int projectileDamage)
    {
        if (shootObj == null || nextEnemy == null)
            return;

        Vector3 startPosition = shootObj.transform.position;
        Vector3 targetPosition = GetEnemyTargetPosition(nextEnemy);
        Vector3 direction = targetPosition - startPosition;

        if (direction.sqrMagnitude <= 0.001f)
        {
            DamageDeal(character, shootObj, nextEnemy, collisionDetector, hitEnemies, projectileDamage);
            return;
        }

        direction.Normalize();
        Vector3 endPosition = GetBoomerangEndPosition(shootObj.transform, startPosition, targetPosition, direction);
        shootObj.transform.rotation = Quaternion.LookRotation(direction);
        collisionDetector.ResetHit();
        shootObj.transform.DOKill();
        MoveProjectile(shootObj, endPosition).OnComplete(() => DestroyShoot(shootObj));
    }

    private Vector3 GetBoomerangEndPosition(Transform projectile, Vector3 startPosition, Vector3 targetPosition,
        Vector3 direction)
    {
        Vector3 desiredEndPosition = targetPosition + direction * BoomerangConfig.OvertravelDistance;
        float raycastSkin = Mathf.Max(0f, BoomerangConfig.RaycastSkin);
        Vector3 rayOrigin = startPosition + direction * raycastSkin;
        float rayDistance = Mathf.Max(0f, Vector3.Distance(startPosition, desiredEndPosition) - raycastSkin);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, direction, rayDistance, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        bool foundBlockingHit = false;
        float closestDistance = float.MaxValue;
        Vector3 blockingPosition = desiredEndPosition;

        foreach (RaycastHit hit in hits)
        {
            if (hit.distance >= closestDistance || !IsBoomerangBlockingCollider(hit.collider, projectile))
                continue;

            foundBlockingHit = true;
            closestDistance = hit.distance;
            blockingPosition = hit.point;
        }

        return foundBlockingHit ? blockingPosition : desiredEndPosition;
    }

    private static bool IsBoomerangBlockingCollider(Collider collider, Transform projectile)
    {
        if (collider == null || collider.isTrigger)
            return false;

        Transform hitTransform = collider.transform;
        if (projectile != null && (hitTransform == projectile || hitTransform.IsChildOf(projectile)))
            return false;

        if (collider.GetComponentInParent<EnemyFacade>() != null ||
            collider.GetComponentInParent<CharacterFacade>() != null ||
            collider.GetComponentInParent<PlayerCollisionDetector>() != null)
            return false;

        return true;
    }

    private int GetBoomerangMaxHitCount() =>
        Mathf.Max(1, BoomerangConfig.StartTargets + _bonusTargets);

    private int GetTargetIncrease(float upgradeMultiplier) =>
        Mathf.Max(0, Mathf.RoundToInt(GetUpgradeValue(BoomerangConfig.TargetUpgradeIncrease, upgradeMultiplier)));

    private int GetTargetsFrom() =>
        Level <= 0 ? 0 : GetBoomerangMaxHitCount();

    private int GetTargetsTo(float upgradeMultiplier)
    {
        int baseTargets = Level <= 0 ? BoomerangConfig.StartTargets : GetBoomerangMaxHitCount();
        return baseTargets + GetTargetIncrease(upgradeMultiplier);
    }

    private float GetDamageIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(BoomerangConfig.DamageUpgradeIncrease, upgradeMultiplier);

    private float GetDamageTo(float upgradeMultiplier) =>
        (Damage <= 0 ? AbilityConfig.StartDamage : Damage) + GetDamageIncrease(upgradeMultiplier);

    private float GetSpeedIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(BoomerangConfig.SpeedUpgradeIncrease, upgradeMultiplier);

    private float GetCooldownReduction(float upgradeMultiplier) =>
        GetUpgradeValue(BoomerangConfig.CooldownUpgradeReduction, upgradeMultiplier);

    private float GetCooldownTo(float upgradeMultiplier) =>
        Mathf.Max(MinimumCooldown, Cooldown - GetCooldownReduction(upgradeMultiplier));

    private float GetBounceRadiusIncrease(float upgradeMultiplier) =>
        GetUpgradeValue(BoomerangConfig.BounceRadiusUpgradeIncrease, upgradeMultiplier);

}
