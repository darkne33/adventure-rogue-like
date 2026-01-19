using UnityEngine;

namespace Features.Enemies.Scripts
{
    public interface IEnemiesProvider
    {
        public void AddEnemy(EnemyFacade enemyFacade);
        public void RemoveEnemy(EnemyFacade enemyFacade);
        public EnemyFacade GetRandomClosestEnemyByCharacter(Transform character, float distance);
    }
}