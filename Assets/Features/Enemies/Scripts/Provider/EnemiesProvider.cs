using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemiesProvider : IEnemiesProvider
    {
        public int Count => _enemies.Count;
        public IReadOnlyList<CombatTarget> ActiveEnemies => _enemies;
        public event Action<int> EnemyRemoved;

        private readonly EnemyRoomObserver _enemyRoomObserver;
        private readonly List<CombatTarget> _enemies = new();
        private bool _isBatchChange;

        public EnemiesProvider(EnemyRoomObserver enemyRoomObserver)
        {
            _enemyRoomObserver = enemyRoomObserver;
        }

        public void AddEnemy(CombatTarget enemyFacade) =>
            _enemies.Add(enemyFacade);

        public void RemoveEnemy(CombatTarget enemyFacade)
        {
            if (_enemies.Remove(enemyFacade) == false)
                return;

            if (_isBatchChange == false)
            {
                EnemyRemoved?.Invoke(_enemies.Count);
                _enemyRoomObserver.Observe(_enemies);
            }
        }

        public int DefeatAllEnemies()
        {
            CombatTarget[] enemies = _enemies.Where(enemy => enemy != null).ToArray();
            _isBatchChange = true;

            try
            {
                foreach (CombatTarget enemy in enemies)
                    enemy.HealthSystem.GetDamage(int.MaxValue);
            }
            finally
            {
                _isBatchChange = false;
            }

            return enemies.Length;
        }

        public int ClearEnemies()
        {
            CombatTarget[] enemies = _enemies.Where(enemy => enemy != null).ToArray();
            _enemies.Clear();

            foreach (CombatTarget enemy in enemies)
                UnityEngine.Object.Destroy(enemy.gameObject);

            return enemies.Length;
        }

        public CombatTarget GetClosestEnemyByCharacter(Transform character, float distance)
        {
            if (character == null || distance <= 0f)
                return null;

            Vector3 characterPosition = character.position;
            float maxSqrDistance = distance * distance;
            float closestSqrDistance = maxSqrDistance;
            CombatTarget closestEnemy = null;

            foreach (CombatTarget enemy in _enemies)
            {
                if (enemy == null || enemy.gameObject.activeInHierarchy == false || enemy.IsDead)
                    continue;

                float sqrDistance = (enemy.transform.position - characterPosition).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                closestEnemy = enemy;
            }

            return closestEnemy;
        }
    }
}
