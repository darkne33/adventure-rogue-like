using Features.Enemies.Scripts;
using UnityEngine;

public interface IEnemyFactory
{
    EnemyFacade Create(GameObject enemy, Vector3 initialPosition, Vector3 navMeshPosition);
}
