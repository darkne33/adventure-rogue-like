namespace Infrastructure.SaveSystem
{
    public interface IPlayerSaveLoadService : ISaveLoadService
    {
        bool HasEncounteredEnemy(EnemyType enemyType);
        void RegisterEncounteredEnemy(EnemyType enemyType);
    }
}
