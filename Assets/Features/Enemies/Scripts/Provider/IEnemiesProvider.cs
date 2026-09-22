using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public interface IEnemiesProvider
    {
        public int Count { get; }
        public IReadOnlyList<CombatTarget> ActiveEnemies { get; }
        public event Action<int> EnemyRemoved;
        public event Action<CombatTarget> EnemyDefeated;
        public void AddEnemy(CombatTarget enemyFacade);
        public void RemoveEnemy(CombatTarget enemyFacade);
        public int DefeatAllEnemies();
        public int ClearEnemies();
        public CombatTarget GetClosestEnemyByCharacter(Transform character, float distance);
    }
}
