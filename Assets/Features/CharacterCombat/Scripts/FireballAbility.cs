using DG.Tweening;
using Features.Enemies.Scripts;
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
        var randomEnemy = _enemiesProvider.GetRandomClosestEnemyByCharacter(character.transform, 100);
        if (randomEnemy == null)
            return;

        var fireball = Object.Instantiate(_abilityConfig.Prefab, character.transform.position, Quaternion.identity);
        fireball.transform.rotation =
            Quaternion.LookRotation(fireball.transform.position - randomEnemy.transform.position);
        fireball.transform.DOMove(randomEnemy.transform.position, 0.7f)
            .OnComplete(() =>
            {
                randomEnemy.HealthSystem.GetDamage(_damage);
                Object.Destroy(fireball);
            });
    }
}