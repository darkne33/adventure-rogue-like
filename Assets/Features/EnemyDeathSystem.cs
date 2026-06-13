using Features.Enemies.Scripts;
using UnityEngine;

public class EnemyDeathSystem : IDeathSystem
{
    private readonly EnemyFacade _enemyFacade;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly ICharacterLevelService _characterLevelService;
    private readonly EnemyConfiguration _enemyConfiguration;
    private readonly CharacterStats _characterStats;
    private readonly CharacterFacade _characterFacade;

    public EnemyDeathSystem(IEnemiesProvider enemiesProvider, EnemyFacade enemyFacade,
        ICharacterLevelService characterLevelService, EnemyConfiguration enemyConfiguration,
        CharacterStats characterStats, CharacterFacade characterFacade)
    {
        _enemyFacade = enemyFacade;
        _characterLevelService = characterLevelService;
        _enemyConfiguration = enemyConfiguration;
        _enemiesProvider = enemiesProvider;
        _characterStats = characterStats;
        _characterFacade = characterFacade;
    }

    public void HandleDeath()
    {
        _enemiesProvider.RemoveEnemy(_enemyFacade);
        _characterLevelService.AddExp(_enemyConfiguration.Exp);
        _characterFacade.HealthSystem.IncreaseCurrentHealth(Mathf.Max(0f, _characterStats.GainHp));
        Object.Destroy(_enemyFacade.gameObject);
    }
}
