using Features.Enemies.Scripts;
using UnityEngine;

public class EnemyDeathSystem : IDeathSystem
{
    private readonly EnemyFacade _enemyFacade;
    private readonly IEnemiesProvider _enemiesProvider;

    public EnemyDeathSystem(IEnemiesProvider enemiesProvider, EnemyFacade enemyFacade)
    {
        _enemyFacade = enemyFacade;
        _enemiesProvider = enemiesProvider;
    }

    public void HandleDeath()
    {
        _enemiesProvider.RemoveEnemy(_enemyFacade);
        Object.Destroy(_enemyFacade.gameObject);
    }
}