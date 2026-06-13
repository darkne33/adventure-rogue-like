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
    
    public EnemyFacade Create(GameObject enemy, Vector3 initialPosition, Vector3 navMeshPosition)
    {
        EnemyFacade enemyFacade = _container.InstantiatePrefabForComponent<EnemyFacade>(
            enemy, initialPosition, Quaternion.identity, null);
        enemyFacade.InitializeNavigation(navMeshPosition);
        return enemyFacade;
    }
}
