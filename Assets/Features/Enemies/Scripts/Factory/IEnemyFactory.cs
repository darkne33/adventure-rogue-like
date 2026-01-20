using Features.Enemies.Scripts;
using UnityEngine;

public interface IEnemyFactory
{
    public EnemyFacade Create(GameObject enemy, Transform spawnPoint);
}