using DG.Tweening;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

public class FireballAbility : SingleShootAbility
{
    private const string DamageStatName = "Damage";

    private FireballAbilityConfiguration FireballConfig => (FireballAbilityConfiguration)AbilityConfig;

    public FireballAbility(IEnemiesProvider enemiesProvider, CharacterDamageCalculator damageCalculator,
        CharacterStats characterStats, RelicEventBus relicEventBus, RelicManager relicManager)
        : base(enemiesProvider, damageCalculator, characterStats, relicEventBus, relicManager)
    {
    }

    public override float GetStatFromIncrease() =>
        Stat_1;

    public override float GetStatToIncrease() =>
        GetStatFromIncrease() + AbilityConfig.StartDamage;

    protected override void OnShootableInitialized()
    {
        StatName_1 = DamageStatName;
    }

    protected override void OnShootableEquipped(CharacterStats characterStats)
    {
        Damage += AbilityConfig.StartDamage;
        Stat_1 = Damage;
    }

    protected override void OnProjectileCreated(CharacterFacade character, GameObject shootObj,
        PlayerCollisionDetector collisionDetector, EnemyFacade targetEnemy, Vector3 spawnPosition,
        Vector3 shootDirection)
    {
        Vector3 endPosition = spawnPosition + shootDirection * FireballConfig.TravelDistance;
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
