namespace Infrastructure.SaveSystem
{
    public interface IPlayerSaveLoadService : ISaveLoadService
    {
        int CompletedRuns { get; }
        void RegisterCompletedRun();
        bool HasEncounteredEnemy(EnemyType enemyType);
        void RegisterEncounteredEnemy(EnemyType enemyType);
    }
}
