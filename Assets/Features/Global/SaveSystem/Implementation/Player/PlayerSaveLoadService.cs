using Infrastructure.SaveSystem.Implementation;
using UnityEngine;

namespace Infrastructure.SaveSystem
{
    public class PlayerSaveLoadService : SaveLoadServiceBase<PlayerGlobalSaveData>, IPlayerSaveLoadService
    {
        private const string EncounteredEnemyKeyPrefix = "little_rush.enemies.encountered.";

        protected override string SaveKey => "Player";
        protected override string CollectionName => "PlayerSaveData";

        public bool HasEncounteredEnemy(EnemyType enemyType) =>
            enemyType != EnemyType.None &&
            PlayerPrefs.GetInt(EncounteredEnemyKeyPrefix + (int)enemyType, 0) != 0;

        public void RegisterEncounteredEnemy(EnemyType enemyType)
        {
            if (enemyType == EnemyType.None || HasEncounteredEnemy(enemyType))
            {
                return;
            }

            PlayerPrefs.SetInt(EncounteredEnemyKeyPrefix + (int)enemyType, 1);
            PlayerPrefs.Save();
        }
    }
}
