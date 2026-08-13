using System.Collections.Generic;
using System.Linq;
using System;
using CustomPackages.Package.Extensions;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemiesProvider : IEnemiesProvider
    {
        private const int ClosestEnemiesPoolSize = 3;

        public int Count => _enemies.Count;
        public IReadOnlyList<EnemyFacade> ActiveEnemies => _enemies;
        public event Action<int> EnemyRemoved;

        private readonly EnemyRoomObserver _enemyRoomObserver;
        private readonly List<EnemyFacade> _enemies = new();
        private bool _isBatchChange;

        public EnemiesProvider(EnemyRoomObserver enemyRoomObserver)
        {
            _enemyRoomObserver = enemyRoomObserver;
        }

        public void AddEnemy(EnemyFacade enemyFacade) =>
            _enemies.Add(enemyFacade);

        public void RemoveEnemy(EnemyFacade enemyFacade)
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
            EnemyFacade[] enemies = _enemies.Where(enemy => enemy != null).ToArray();
            _isBatchChange = true;

            try
            {
                foreach (EnemyFacade enemy in enemies)
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
            EnemyFacade[] enemies = _enemies.Where(enemy => enemy != null).ToArray();
            _enemies.Clear();

            foreach (EnemyFacade enemy in enemies)
                UnityEngine.Object.Destroy(enemy.gameObject);

            return enemies.Length;
        }

        public EnemyFacade GetRandomClosestEnemyByCharacter(Transform character, float distance)
        {
            if (character == null || distance <= 0f)
                return null;

            Vector3 characterPosition = character.position;
            float maxSqrDistance = distance * distance;
            List<EnemyFacade> closestEnemies = _enemies
                .Where(enemy => enemy != null &&
                                enemy.gameObject.activeInHierarchy &&
                                enemy.IsDead == false &&
                                (enemy.transform.position - characterPosition).sqrMagnitude < maxSqrDistance)
                .OrderBy(enemy => (enemy.transform.position - characterPosition).sqrMagnitude)
                .Take(ClosestEnemiesPoolSize)
                .ToList();

            return closestEnemies.GetRandom();
        }
    }
}
