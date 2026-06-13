using System.Collections.Generic;
using System.Linq;
using CustomPackages.Package.Extensions;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemiesProvider : IEnemiesProvider
    {
        public int Count => _enemies.Count;

        private readonly EnemiesWaveObserver _enemiesWaveObserver;
        private readonly List<EnemyFacade> _enemies = new();
        private bool _isBatchChange;

        public EnemiesProvider(EnemiesWaveObserver enemiesWaveObserver)
        {
            _enemiesWaveObserver = enemiesWaveObserver;
        }

        public void AddEnemy(EnemyFacade enemyFacade) =>
            _enemies.Add(enemyFacade);

        public void RemoveEnemy(EnemyFacade enemyFacade)
        {
            _enemies.Remove(enemyFacade);

            if (_isBatchChange == false)
                _enemiesWaveObserver.Observe(_enemies);
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
                Object.Destroy(enemy.gameObject);

            return enemies.Length;
        }

        public EnemyFacade GetRandomClosestEnemyByCharacter(Transform character, float distance)
        {
            var closestEnemies = _enemies
                .Where(x => (x.transform.position - character.transform.position).magnitude < distance).ToList();
            var randomEnemy = closestEnemies.GetRandom();
            return randomEnemy;
        }
    }
}
