using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

public class EnemyFactory : IEnemyFactory
{
    private readonly DiContainer _container;
    
    public EnemyFactory(DiContainer container)
    {
        _container = container;
    }
    
    public EnemyFacade Create(GameObject enemy, Transform spawnPoint)
    {
        var enemyFacade =
            _container.InstantiatePrefabForComponent<EnemyFacade>(enemy, spawnPoint);
        return enemyFacade;
    }
}