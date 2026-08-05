using DG.Tweening;
using Features.Enemies.Scripts;
using UnityEngine;

public class EnemyDeathSystem : IDeathSystem
{
    private const float DEATH_FADE_DURATION = 0.25f;

    private readonly EnemyFacade _enemyFacade;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly EnemyConfiguration _enemyConfiguration;
    private readonly CharacterStats _characterStats;
    private readonly CharacterFacade _characterFacade;
    private readonly DealDamageEffectSystem _effectsSystem;
    private readonly System.Action _deathEffect;
    private readonly GoldDropper _goldDropper;
    private readonly HeartDropper _heartDropper;
    private readonly ExpDropper _expDropper;

    public EnemyDeathSystem(IEnemiesProvider enemiesProvider, EnemyFacade enemyFacade,
        EnemyConfiguration enemyConfiguration, CharacterStats characterStats, CharacterFacade characterFacade,
        DealDamageEffectSystem effectsSystem, System.Action deathEffect, GoldDropper goldDropper,
        HeartDropper heartDropper, ExpDropper expDropper)
    {
        _enemyFacade = enemyFacade;
        _enemyConfiguration = enemyConfiguration;
        _enemiesProvider = enemiesProvider;
        _characterStats = characterStats;
        _characterFacade = characterFacade;
        _effectsSystem = effectsSystem;
        _deathEffect = deathEffect;
        _goldDropper = goldDropper;
        _heartDropper = heartDropper;
        _expDropper = expDropper;
    }

    public void HandleDeath()
    {
        _deathEffect?.Invoke();
        _enemiesProvider.RemoveEnemy(_enemyFacade);
        _expDropper.DropExp(_enemyFacade.transform.position, CalculateExpReward(_enemyConfiguration.Exp));
        _characterFacade.HealthSystem.IncreaseCurrentHealth(Mathf.Max(0f, _characterStats.GainHp));
        _goldDropper.DropGold(_enemyFacade.transform.position);
        _heartDropper.TryDropHeart(_enemyFacade.transform.position);

        PrepareForDeathAnimation();

        Tween deathFadeTween = _effectsSystem?.PlayDeathFade(DEATH_FADE_DURATION);
        if (deathFadeTween == null)
        {
            DestroyEnemy();
            return;
        }

        deathFadeTween.OnComplete(DestroyEnemy);
    }

    private void PrepareForDeathAnimation()
    {
        _enemyFacade.SetStop(true);
        _enemyFacade.AnimationSystem?.IdleAnimation();

        if (_enemyFacade.EnemyCollisionDetector != null)
            _enemyFacade.EnemyCollisionDetector.enabled = false;

        Rigidbody rigidbody = _enemyFacade.Rigidbody;
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        Collider[] colliders = _enemyFacade.GetComponentsInChildren<Collider>();
        foreach (Collider enemyCollider in colliders)
            enemyCollider.enabled = false;
    }

    private void DestroyEnemy()
    {
        if (_enemyFacade != null)
            Object.Destroy(_enemyFacade.gameObject);
    }

    private int CalculateExpReward(int baseReward)
    {
        float scaledReward = baseReward * (1f + Mathf.Max(0f, _characterStats.XPBonus) * 0.01f);
        int reward = Mathf.FloorToInt(scaledReward);

        if (Random.value < scaledReward - reward)
            reward++;

        return Mathf.Max(0, reward);
    }
}
