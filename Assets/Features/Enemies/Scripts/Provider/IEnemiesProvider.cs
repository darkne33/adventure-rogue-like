using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public interface IEnemiesProvider
    {
        public int Count { get; }
        public IReadOnlyList<EnemyFacade> ActiveEnemies { get; }
        public event Action<int> EnemyRemoved;
        public void AddEnemy(EnemyFacade enemyFacade);
        public void RemoveEnemy(EnemyFacade enemyFacade);
        public int DefeatAllEnemies();
        public int ClearEnemies();
        public EnemyFacade GetRandomClosestEnemyByCharacter(Transform character, float distance);
    }
}
