using DG.Tweening;
using Features.Enemies.Scripts;
using UniRx.Triggers;
using UnityEngine;

public class FireballAbility : CharacterActiveAbility
{
    private int _damage;

    private FireballAbilityConfiguration _abilityConfig;
    private readonly IEnemiesProvider _enemiesProvider;

    public FireballAbility(IEnemiesProvider enemiesProvider)
    {
        _enemiesProvider = enemiesProvider;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        _abilityConfig = (FireballAbilityConfiguration)abilityConfig;
        _damage = _abilityConfig.StartDamage;
        Cooldown = _abilityConfig.Cooldown;
    }

    protected override void OnUse(CharacterFacade character)
    {
        EnemyFacade randomEnemy = _enemiesProvider.GetRandomClosestEnemyByCharacter(character.transform, 100);
        if (randomEnemy == null)
            return;

        var randomEnemyPosition = randomEnemy.transform.position;

        var fireball = Object.Instantiate(_abilityConfig.Prefab, character.transform.position, Quaternion.identity);
        fireball.transform.rotation =
            Quaternion.LookRotation(fireball.transform.position - randomEnemyPosition);

        fireball.transform.DOMove(randomEnemyPosition, 50).SetSpeedBased().SetLink(fireball).SetId("Fireball Ability");
        
        CollisionDetector collisionDetector = fireball.GetComponent<CollisionDetector>();
        collisionDetector.OnCollisionEnter = enemyFacade => DamageDeal(fireball, enemyFacade);
    }
    private void DamageDeal(GameObject fireball, EnemyFacade enemyFacade)
    {
        enemyFacade.HealthSystem.GetDamage(_damage);
        enemyFacade.EffectsSystem.DealDamage();
        Object.Destroy(fireball);
    }
}