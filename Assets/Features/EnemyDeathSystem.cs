using Features.Enemies.Scripts;
using UnityEngine;

public class EnemyDeathSystem : IDeathSystem
{
    private readonly EnemyFacade _enemyFacade;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly ICharacterLevelService _characterLevelService;
    private readonly EnemyConfiguration _enemyConfiguration;

    public EnemyDeathSystem(IEnemiesProvider enemiesProvider, EnemyFacade enemyFacade,
        ICharacterLevelService characterLevelService, EnemyConfiguration enemyConfiguration)
    {
        _enemyFacade = enemyFacade;
        _characterLevelService = characterLevelService;
        _enemyConfiguration = enemyConfiguration;
        _enemiesProvider = enemiesProvider;
    }

    public void HandleDeath()
    {
        _enemiesProvider.RemoveEnemy(_enemyFacade);
        _characterLevelService.AddExp(_enemyConfiguration.Exp);
        Object.Destroy(_enemyFacade.gameObject);
    }
}