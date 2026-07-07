using CustomPackages.Package.Extensions;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public class SingleShootAbility : CharacterActiveAbility
{
    private const float ProjectileSpreadOffset = 0.35f;
    private const float ProjectileTravelDistance = 100f;
    private const float BoomerangBounceRadius = 12f;
    private const float BoomerangOvertravelDistance = 8f;
    private const float BoomerangRaycastSkin = 0.05f;
    private const string DamageStatName = "Damage";
    private const string BoomerangStatName = "Targets";

    private int _damage;

    private ShootableAbilityConfiguration _abilityConfig;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _characterStats;
    private readonly RelicEventBus _relicEventBus;
    private readonly RelicManager _relicManager;

    public SingleShootAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
        CharacterStats characterStats, RelicEventBus relicEventBus, RelicManager relicManager)
    {
        _enemiesProvider = enemiesProvider;
        _damageCalculator = damageCalculator;
        _characterStats = characterStats;
        _relicEventBus = relicEventBus;
        _relicManager = relicManager;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);

        if (IsBoomerang)
        {
            if (_damage <= 0)
                _damage = _abilityConfig.StartDamage;

            Stat_1 = GetBoomerangMaxHitCount();
            return;
        }

        _damage += _abilityConfig.StartDamage;
        Stat_1 = _damage;
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        _abilityConfig = (ShootableAbilityConfiguration)abilityConfig;

        if (IsBoomerang)
        {
            Level = 0;
            StatName_1 = BoomerangStatName;
            Stat_1 = 1;
        }
        else
        {
            StatName_1 = DamageStatName;
        }

        Cooldown = _abilityConfig.Cooldown;
    }

    protected override void OnUse(CharacterFacade character)
    {
        int projectileCount = CalculateProjectileCount();
        for (int index = 0; index < projectileCount; index++)
            ShootProjectile(character, index, projectileCount);
    }

    private void ShootProjectile(CharacterFacade character, int projectileIndex, int projectileCount)
    {
        EnemyFacade randomEnemy = _enemiesProvider.GetRandomClosestEnemyByCharacter(character.transform, 100);

        if (randomEnemy == null)
            return;

        Vector3 targetPosition = GetEnemyTargetPosition(randomEnemy);
        Vector3 spawnPosition = character.transform.position +
                                GetProjectileSpawnOffset(character.transform, projectileIndex, projectileCount);
        Vector3 shootDirection = targetPosition - spawnPosition;
        shootDirection.y = 0f;
        if (shootDirection.sqrMagnitude <= 0.001f)
            shootDirection = character.transform.forward;
        shootDirection.Normalize();

        var shootObj = Object.Instantiate(_abilityConfig.Prefab, spawnPosition, Quaternion.identity);
        shootObj.transform.rotation = Quaternion.LookRotation(shootDirection);

        var playerCollisionDetector = shootObj.GetComponent<PlayerCollisionDetector>();
        if (playerCollisionDetector == null)
        {
            DestroyShoot(shootObj);
            return;
        }

        playerCollisionDetector.Initialize(character.transform);
        if (IsBoomerang)
        {
            HashSet<EnemyFacade> hitEnemies = new();
            playerCollisionDetector.OnHit = enemyFacade =>
                DamageDeal(character, shootObj, enemyFacade, playerCollisionDetector, hitEnemies);
            MoveBoomerangToEnemy(character, shootObj, playerCollisionDetector, hitEnemies, randomEnemy);
            return;
        }

        Vector3 endPosition = spawnPosition + shootDirection * ProjectileTravelDistance;
        shootObj.transform.DOMove(endPosition, _abilityConfig.Speed).SetSpeedBased().SetLink(shootObj)
            .SetId($"Shoot Ability {shootObj.name}")
            .OnComplete(() => DestroyShoot(shootObj));

        playerCollisionDetector.OnHit = enemyFacade => DamageDeal(character, shootObj, enemyFacade);
    }

    public override float GetStatFromIncrease() =>
        IsBoomerang ? GetBoomerangMaxHitCount() : Stat_1;

    public override float GetStatToIncrease() =>
        IsBoomerang ? GetBoomerangMaxHitCount() + 1 : GetStatFromIncrease() + _abilityConfig.StartDamage;

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

    private void ApplyDamage(CharacterFacade character, EnemyFacade enemyFacade)
    {
        CharacterDamageResult damageResult = _damageCalculator.Calculate(_damage);
        int finalDamage = _relicManager.ModifyOutgoingDamage(damageResult.Damage, enemyFacade);
        int appliedDamage = enemyFacade.HealthSystem.GetDamage(finalDamage, damageResult.IsCritical);
        bool killedByDirectHit = enemyFacade.HealthSystem.IsDead;

        if (appliedDamage <= 0)
            return;

        float lifeStealPercent = Mathf.Max(0f, _characterStats.LifeSteal) * 0.01f;
        float healed = character.HealthSystem.IncreaseCurrentHealth(appliedDamage * lifeStealPercent);
        if (healed > 0f)
            _relicEventBus.PublishHeal(new RelicHealEvent(character, healed));

        enemyFacade.EffectsSystem.DealDamage();
        _relicEventBus.PublishHit(new RelicHitEvent(character, enemyFacade, appliedDamage,
            damageResult.IsCritical, _abilityConfig.AbilityName.ToString(), enemyFacade.transform.position));

        if (killedByDirectHit)
            _relicEventBus.PublishKill(new RelicKillEvent(character, enemyFacade, enemyFacade.transform.position));
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
        float maxSqrDistance = BoomerangBounceRadius * BoomerangBounceRadius;

        foreach (EnemyFacade enemy in _enemiesProvider.ActiveEnemies)
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
        shootObj.transform.DOMove(endPosition, _abilityConfig.Speed).SetSpeedBased().SetLink(shootObj)
            .SetId($"Shoot Ability {shootObj.name}")
            .OnComplete(() => DestroyShoot(shootObj));
    }

    private static Vector3 GetBoomerangEndPosition(Transform projectile, Vector3 startPosition, Vector3 targetPosition,
        Vector3 direction)
    {
        Vector3 desiredEndPosition = targetPosition + direction * BoomerangOvertravelDistance;
        Vector3 rayOrigin = startPosition + direction * BoomerangRaycastSkin;
        float rayDistance = Mathf.Max(0f, Vector3.Distance(startPosition, desiredEndPosition) - BoomerangRaycastSkin);
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

    private static Vector3 GetEnemyTargetPosition(EnemyFacade enemy) =>
        enemy.TargetToShootDamage != null ? enemy.TargetToShootDamage.position : enemy.transform.position;

    private int GetBoomerangMaxHitCount() =>
        Mathf.Max(1, Level);

    private bool IsBoomerang =>
        _abilityConfig != null && _abilityConfig.AbilityName == AbilityName.RabbitBoomerang;

    private int CalculateProjectileCount()
    {
        float projectileBonus = Mathf.Max(0f, _characterStats.ProjectileCount);
        int projectileCount = 1 + Mathf.FloorToInt(projectileBonus);
        float fractionalProjectile = projectileBonus - Mathf.Floor(projectileBonus);

        if (Random.value < fractionalProjectile)
            projectileCount++;

        return Mathf.Max(1, projectileCount);
    }

    private static Vector3 GetProjectileSpawnOffset(Transform characterTransform, int projectileIndex,
        int projectileCount)
    {
        if (projectileCount <= 1)
            return Vector3.zero;

        float centeredIndex = projectileIndex - (projectileCount - 1) * 0.5f;
        return characterTransform.right * centeredIndex * ProjectileSpreadOffset;
    }

    private void DestroyShoot(GameObject shootObj)
    {
        if (shootObj != null)
        {
            var explosion = Object.Instantiate(_abilityConfig.ExplosionPrefab, shootObj.transform.position,
                Quaternion.identity);
            
            var muzzle = Object.Instantiate(_abilityConfig.MuzzlePrefab, shootObj.transform.position,
                Quaternion.identity);
            
            Object.Destroy(shootObj);
            
            float effectDuration = AbilityDurationMultiplier;
            DestroyExtensions.DestroyAfterDelay(explosion, effectDuration,
                explosion.GetCancellationTokenOnDestroy()).Forget();
            DestroyExtensions.DestroyAfterDelay(muzzle, effectDuration,
                muzzle.GetCancellationTokenOnDestroy()).Forget();
        }
    }

}
