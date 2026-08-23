using CustomPackages.Package.Extensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public abstract class SingleShootAbility : CharacterActiveAbility
{
    private const float ProjectileSpreadOffset = 0.35f;
    private const float AutoTargetingDistance = 100f;
    private const string ProjectileCountStatName = "Projectiles";
    protected const float MinimumCooldown = 0.05f;

    private readonly IEnemiesProvider _enemiesProvider;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _characterStats;
    private readonly RelicEventBus _relicEventBus;
    private readonly RelicManager _relicManager;

    private float _additionalProjectileCount;
    private bool _isLaunchingProjectiles;
    private int _projectileLaunchSequence;

    protected ShootableAbilityConfiguration AbilityConfig { get; private set; }
    protected IEnemiesProvider EnemiesProvider => _enemiesProvider;
    protected int Damage { get; set; }
    protected float ProjectileSpeed { get; private set; }
    protected virtual int BaseProjectileCount => 1;

    protected SingleShootAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
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
        _projectileLaunchSequence++;
        _isLaunchingProjectiles = false;
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

        ApplyAdditionalProjectileUpgrade(CurrentPrimaryUpgrade);
        if (CurrentSecondaryUpgrade.HasValue)
            ApplyAdditionalProjectileUpgrade(CurrentSecondaryUpgrade.Value);

        OnShootableEquipped(characterStats);
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        _projectileLaunchSequence++;
        _isLaunchingProjectiles = false;
        base.OnUnequip(characterStats);
        _additionalProjectileCount = 0f;
    }

    protected override bool IsReady(CharacterFacade character) =>
        _isLaunchingProjectiles == false &&
        base.IsReady(character);

    protected override void OnUse(CharacterFacade character)
    {
        int projectileCount = CalculateProjectileCount();
        int launchSequence = ++_projectileLaunchSequence;
        _isLaunchingProjectiles = true;
        LaunchProjectiles(character, projectileCount, launchSequence).Forget();
    }

    protected virtual void OnShootableInitialized()
    {
    }

    protected virtual void OnShootableEquipped(CharacterStats characterStats)
    {
    }

    protected AbilityUpgradePreview GetAdditionalProjectileUpgradePreview(AbilityUpgradeEffect upgrade)
    {
        float projectileCount = GetCurrentProjectileCount();
        return new AbilityUpgradePreview(ProjectileCountStatName, projectileCount,
            projectileCount + GetAdditionalProjectileIncrease(upgrade.Value));
    }

    protected abstract void OnProjectileCreated(CharacterFacade character, GameObject shootObj,
        PlayerCollisionDetector collisionDetector, EnemyFacade targetEnemy, Vector3 spawnPosition,
        Vector3 shootDirection, int projectileDamage);

    protected virtual int GetProjectileDamage(int projectileIndex, int projectileCount) =>
        Damage;

    protected void ApplyDamage(CharacterFacade character, EnemyFacade enemyFacade) =>
        ApplyDamage(character, enemyFacade, Damage);

    protected void ApplyDamage(CharacterFacade character, EnemyFacade enemyFacade, int baseDamage)
    {
        if (baseDamage <= 0)
            return;

        CharacterDamageResult damageResult = _damageCalculator.Calculate(GetRolledDamage(baseDamage));
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
        _relicEventBus.PublishHit(new RelicHitEvent(character, enemyFacade, finalDamage,
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
        Cooldown = Mathf.Max(MinimumCooldown, Cooldown - Mathf.Max(0f, cooldownReduction));
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

    private int GetRolledDamage(int baseDamage)
    {
        float variation = Mathf.Max(0f, AbilityConfig.DamageVariationPercent) * 0.01f;
        float multiplier = Random.Range(1f - variation, 1f + variation);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }

    private async UniTask LaunchProjectiles(CharacterFacade character, int projectileCount, int launchSequence)
    {
        try
        {
            float launchDelay = Mathf.Max(0f, AbilityConfig.AdditionalProjectileLaunchDelay);
            for (int index = 0; index < projectileCount; index++)
            {
                if (index > 0 && launchDelay > 0f)
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(launchDelay),
                        cancellationToken: character.GetCancellationTokenOnDestroy());
                }

                if (launchSequence != _projectileLaunchSequence)
                    return;

                ShootProjectile(character, index, projectileCount);
            }
        }
        finally
        {
            if (launchSequence == _projectileLaunchSequence)
                _isLaunchingProjectiles = false;
        }
    }

    private void ShootProjectile(CharacterFacade character, int projectileIndex, int projectileCount)
    {
        EnemyFacade targetEnemy = _enemiesProvider.GetClosestEnemyByCharacter(character.transform,
            AutoTargetingDistance);

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
        int projectileDamage = GetProjectileDamage(projectileIndex, projectileCount);
        OnProjectileCreated(character, shootObj, playerCollisionDetector, targetEnemy, spawnPosition, shootDirection,
            projectileDamage);
    }

    private int CalculateProjectileCount()
    {
        float projectileBonus = Mathf.Max(0f, _characterStats.ProjectileCount) +
                                Mathf.Max(0f, _additionalProjectileCount);
        int projectileCount = Mathf.Max(1, BaseProjectileCount) + Mathf.FloorToInt(projectileBonus);
        float fractionalProjectile = projectileBonus - Mathf.Floor(projectileBonus);

        if (Random.value < fractionalProjectile)
            projectileCount++;

        return Mathf.Max(1, projectileCount);
    }

    private float GetCurrentProjectileCount() =>
        Mathf.Max(1, BaseProjectileCount) + _additionalProjectileCount +
        Mathf.Max(0f, _characterStats.ProjectileCount);

    private void ApplyAdditionalProjectileUpgrade(AbilityUpgradeEffect upgrade)
    {
        if (upgrade.Type == AbilityUpgradeType.AdditionalProjectiles)
            _additionalProjectileCount += GetAdditionalProjectileIncrease(upgrade.Value);
    }

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
