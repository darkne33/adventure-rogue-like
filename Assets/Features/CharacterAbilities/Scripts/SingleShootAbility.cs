using CustomPackages.Package.Extensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public abstract class SingleShootAbility : CharacterActiveAbility
{
    private const float ProjectileSpreadOffset = 0.35f;
    private const string ProjectileCountStatName = "Projectiles";

    private readonly IEnemiesProvider _enemiesProvider;
    private readonly ICharacterAimTargetProvider _aimTargetProvider;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _characterStats;
    private readonly RelicEventBus _relicEventBus;
    private readonly RelicManager _relicManager;

    private float _additionalProjectileCount;

    protected ShootableAbilityConfiguration AbilityConfig { get; private set; }
    protected IEnemiesProvider EnemiesProvider => _enemiesProvider;
    protected int Damage { get; set; }
    protected float ProjectileSpeed { get; private set; }

    protected SingleShootAbility(IEnemiesProvider enemiesProvider, ICharacterAimTargetProvider aimTargetProvider,
        CharacterDamageCalculator damageCalculator, CharacterStats characterStats, RelicEventBus relicEventBus,
        RelicManager relicManager)
    {
        _enemiesProvider = enemiesProvider;
        _aimTargetProvider = aimTargetProvider;
        _damageCalculator = damageCalculator;
        _characterStats = characterStats;
        _relicEventBus = relicEventBus;
        _relicManager = relicManager;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        AbilityConfig = (ShootableAbilityConfiguration)abilityConfig;
        Cooldown = AbilityConfig.Cooldown;
        ProjectileSpeed = AbilityConfig.Speed;
        _additionalProjectileCount = 0f;
        OnShootableInitialized();
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);

        if (CurrentUpgradeType == AbilityUpgradeType.AdditionalProjectiles)
            _additionalProjectileCount += GetAdditionalProjectileIncrease(CurrentUpgradeMultiplier);

        OnShootableEquipped(characterStats);
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        _additionalProjectileCount = 0f;
    }

    protected override void OnUse(CharacterFacade character)
    {
        EnemyFacade aimedEnemy = _aimTargetProvider.GetAimedEnemy();
        int projectileCount = CalculateProjectileCount();
        for (int index = 0; index < projectileCount; index++)
            ShootProjectile(character, aimedEnemy, index, projectileCount);
    }

    protected virtual void OnShootableInitialized()
    {
    }

    protected virtual void OnShootableEquipped(CharacterStats characterStats)
    {
    }

    protected AbilityUpgradePreview[] GetAdditionalProjectileUpgradePreviews(float upgradeValue)
    {
        float projectileCount = GetCurrentProjectileCount();
        return new[]
        {
            new AbilityUpgradePreview(ProjectileCountStatName, projectileCount,
                projectileCount + GetAdditionalProjectileIncrease(upgradeValue))
        };
    }

    protected abstract void OnProjectileCreated(CharacterFacade character, GameObject shootObj,
        PlayerCollisionDetector collisionDetector, EnemyFacade targetEnemy, Vector3 spawnPosition,
        Vector3 shootDirection);

    protected void ApplyDamage(CharacterFacade character, EnemyFacade enemyFacade)
    {
        CharacterDamageResult damageResult = _damageCalculator.Calculate(GetRolledDamage());
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
            damageResult.IsCritical, AbilityConfig.AbilityName.ToString(), enemyFacade.transform.position));

        if (killedByDirectHit)
            _relicEventBus.PublishKill(new RelicKillEvent(character, enemyFacade, enemyFacade.transform.position));
    }

    protected Tween MoveProjectile(GameObject shootObj, Vector3 endPosition) =>
        shootObj.transform.DOMove(endPosition, ProjectileSpeed).SetSpeedBased().SetLink(shootObj)
            .SetId($"Shoot Ability {shootObj.name}");

    protected void IncreaseDamage(float damageIncrease)
    {
        Damage += Mathf.Max(0, Mathf.RoundToInt(damageIncrease));
    }

    protected void IncreaseProjectileSpeed(float speedIncrease)
    {
        ProjectileSpeed += Mathf.Max(0f, speedIncrease);
    }

    protected void ReduceCooldown(float cooldownReduction)
    {
        Cooldown = Mathf.Max(0.05f, Cooldown - Mathf.Max(0f, cooldownReduction));
    }

    protected void DestroyShoot(GameObject shootObj)
    {
        if (shootObj == null)
            return;

        Vector3 effectPosition = shootObj.transform.position;
        Object.Destroy(shootObj);
        SpawnEffect(AbilityConfig.ExplosionPrefab, effectPosition);
        SpawnEffect(AbilityConfig.MuzzlePrefab, effectPosition);
    }

    protected static Vector3 GetEnemyTargetPosition(EnemyFacade enemy) =>
        enemy.TargetToShootDamage != null ? enemy.TargetToShootDamage.position : enemy.transform.position;

    private int GetRolledDamage()
    {
        float variation = Mathf.Max(0f, AbilityConfig.DamageVariationPercent) * 0.01f;
        float multiplier = Random.Range(1f - variation, 1f + variation);
        return Mathf.Max(1, Mathf.RoundToInt(Damage * multiplier));
    }

    private void ShootProjectile(CharacterFacade character, EnemyFacade aimedEnemy, int projectileIndex,
        int projectileCount)
    {
        EnemyFacade targetEnemy = IsValidAimedEnemy(aimedEnemy, character)
            ? aimedEnemy
            : _enemiesProvider.GetRandomClosestEnemyByCharacter(character.transform,
                _aimTargetProvider.TargetingDistance);

        if (targetEnemy == null)
            return;

        Vector3 targetPosition = GetEnemyTargetPosition(targetEnemy);
        Vector3 spawnPosition = character.ProjectileSpawnPosition +
                                GetProjectileSpawnOffset(character.transform, projectileIndex, projectileCount);
        Vector3 shootDirection = targetPosition - spawnPosition;
        if (shootDirection.sqrMagnitude <= 0.001f)
            shootDirection = character.transform.forward;
        shootDirection.Normalize();

        GameObject shootObj = Object.Instantiate(AbilityConfig.Prefab, spawnPosition, Quaternion.identity);
        shootObj.transform.rotation = Quaternion.LookRotation(shootDirection);

        PlayerCollisionDetector playerCollisionDetector = shootObj.GetComponent<PlayerCollisionDetector>();
        if (playerCollisionDetector == null)
        {
            DestroyShoot(shootObj);
            return;
        }

        playerCollisionDetector.Initialize(character.transform);
        OnProjectileCreated(character, shootObj, playerCollisionDetector, targetEnemy, spawnPosition, shootDirection);
    }

    private bool IsValidAimedEnemy(EnemyFacade aimedEnemy, CharacterFacade character)
    {
        if (aimedEnemy == null || aimedEnemy.gameObject.activeInHierarchy == false || aimedEnemy.IsDead)
            return false;

        float maxSqrDistance = _aimTargetProvider.TargetingDistance * _aimTargetProvider.TargetingDistance;
        return (aimedEnemy.transform.position - character.transform.position).sqrMagnitude < maxSqrDistance;
    }

    private int CalculateProjectileCount()
    {
        float projectileBonus = Mathf.Max(0f, _characterStats.ProjectileCount) +
                                Mathf.Max(0f, _additionalProjectileCount);
        int projectileCount = 1 + Mathf.FloorToInt(projectileBonus);
        float fractionalProjectile = projectileBonus - Mathf.Floor(projectileBonus);

        if (Random.value < fractionalProjectile)
            projectileCount++;

        return Mathf.Max(1, projectileCount);
    }

    private float GetCurrentProjectileCount() =>
        1f + _additionalProjectileCount + Mathf.Max(0f, _characterStats.ProjectileCount);

    private static float GetAdditionalProjectileIncrease(float upgradeValue) =>
        Mathf.Max(0f, upgradeValue);

    private static Vector3 GetProjectileSpawnOffset(Transform characterTransform, int projectileIndex,
        int projectileCount)
    {
        if (projectileCount <= 1)
            return Vector3.zero;

        float centeredIndex = projectileIndex - (projectileCount - 1) * 0.5f;
        return characterTransform.right * centeredIndex * ProjectileSpreadOffset;
    }

    private void SpawnEffect(GameObject effectPrefab, Vector3 position)
    {
        if (effectPrefab == null)
            return;

        GameObject effect = Object.Instantiate(effectPrefab, position, Quaternion.identity);
        DestroyExtensions.DestroyAfterDelay(effect, AbilityDurationMultiplier,
            effect.GetCancellationTokenOnDestroy()).Forget();
    }
}
