using System.Collections.Generic;
using System.Linq;
using CustomPackages.Package.Extensions;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemiesProvider : IEnemiesProvider
    {
        private readonly List<EnemyFacade> _enemies = new List<EnemyFacade>();

        public void AddEnemy(EnemyFacade enemyFacade) =>
            _enemies.Add(enemyFacade);

        public void RemoveEnemy(EnemyFacade enemyFacade) =>
            _enemies.Remove(enemyFacade);

        public EnemyFacade GetRandomClosestEnemyByCharacter(Transform character, float distance)
        {
            var closestEnemies = _enemies
                .Where(x => (x.transform.position - character.transform.position).magnitude < distance).ToList();
            var randomEnemy = closestEnemies.GetRandom();
            return randomEnemy;
        }
    }
}