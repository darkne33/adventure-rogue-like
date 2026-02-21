using System.Collections.Generic;
using System.Linq;
using CustomPackages.Package.Extensions;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemiesProvider : IEnemiesProvider
    {
        private readonly EnemiesWaveObserver _enemiesWaveObserver;
        
        private readonly List<EnemyFacade> _enemies = new();

        public EnemiesProvider(EnemiesWaveObserver enemiesWaveObserver)
        {
            _enemiesWaveObserver = enemiesWaveObserver;
        }

        public void AddEnemy(EnemyFacade enemyFacade) =>
            _enemies.Add(enemyFacade);

        public void RemoveEnemy(EnemyFacade enemyFacade)
        {
            _enemies.Remove(enemyFacade);
            _enemiesWaveObserver.Observe(_enemies);
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