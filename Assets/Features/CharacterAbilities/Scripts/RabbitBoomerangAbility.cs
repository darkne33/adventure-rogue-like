using System.Collections.Generic;
using DG.Tweening;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public class RabbitBoomerangAbility : SingleShootAbility
{
    private const string BoomerangStatName = "Targets";

    private RabbitBoomerangAbilityConfiguration BoomerangConfig =>
        (RabbitBoomerangAbilityConfiguration)AbilityConfig;

    public RabbitBoomerangAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
        CharacterStats characterStats, RelicEventBus relicEventBus, RelicManager relicManager)
        : base(enemiesProvider, damageCalculator, characterStats, relicEventBus, relicManager)
    {
    }

    public override float GetStatFromIncrease() =>
        GetBoomerangMaxHitCount();

    public override float GetStatToIncrease() =>
        GetBoomerangHitCount(Level + 1);

    protected override void OnShootableInitialized()
    {
        Level = 0;
        StatName_1 = BoomerangStatName;
        Stat_1 = GetBoomerangHitCount(1);
    }

    protected override void OnShootableEquipped(CharacterStats characterStats)
    {
        if (Damage <= 0)
            Damage = AbilityConfig.StartDamage;

        Stat_1 = GetBoomerangMaxHitCount();
    }

    protected override void OnProjectileCreated(CharacterFacade character, GameObject shootObj,
        PlayerCollisionDetector collisionDetector, EnemyFacade targetEnemy, Vector3 spawnPosition,
        Vector3 shootDirection)
    {
        HashSet<EnemyFacade> hitEnemies = new();
        collisionDetector.OnHit = enemyFacade =>
            DamageDeal(character, shootObj, enemyFacade, collisionDetector, hitEnemies);
        MoveBoomerangToEnemy(character, shootObj, collisionDetector, hitEnemies, targetEnemy);
    }

    private void DamageDeal(CharacterFacade character, GameObject shootObj, EnemyFacade enemyFacade,
        PlayerCollisionDetector collisionDetector, HashSet<EnemyFacade> hitEnemies)
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
        ApplyDamage(character, enemyFacade);

        if (hitEnemies.Count >= GetBoomerangMaxHitCount())
        {
            DestroyShoot(shootObj);
            return;
        }

        TryBounceBoomerang(character, shootObj, collisionDetector, hitEnemies);
    }

    private void TryBounceBoomerang(CharacterFacade character, GameObject shootObj,
        PlayerCollisionDetector collisionDetector, HashSet<EnemyFacade> hitEnemies)
    {
        EnemyFacade nextEnemy = FindNextBoomerangTarget(shootObj.transform.position, hitEnemies);
        if (nextEnemy == null)
        {
            DestroyShoot(shootObj);
            return;
        }

        MoveBoomerangToEnemy(character, shootObj, collisionDetector, hitEnemies, nextEnemy);
    }

    private EnemyFacade FindNextBoomerangTarget(Vector3 projectilePosition, HashSet<EnemyFacade> hitEnemies)
    {
        EnemyFacade closestEnemy = null;
        float closestSqrDistance = float.MaxValue;
        float maxSqrDistance = BoomerangConfig.BounceRadius * BoomerangConfig.BounceRadius;

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
        PlayerCollisionDetector collisionDetector, HashSet<EnemyFacade> hitEnemies, EnemyFacade nextEnemy)
    {
        if (shootObj == null || nextEnemy == null)
            return;

        Vector3 startPosition = shootObj.transform.position;
        Vector3 targetPosition = GetEnemyTargetPosition(nextEnemy);
        Vector3 direction = targetPosition - startPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            DamageDeal(character, shootObj, nextEnemy, collisionDetector, hitEnemies);
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
        GetBoomerangHitCount(Level);

    private int GetBoomerangHitCount(int level)
    {
        int upgradeCount = Mathf.Max(0, level - 1);
        int targetCount = BoomerangConfig.StartTargets + upgradeCount * BoomerangConfig.TargetsPerLevel;
        return Mathf.Max(1, targetCount);
    }
}
