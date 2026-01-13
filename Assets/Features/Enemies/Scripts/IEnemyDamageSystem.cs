namespace Features.Enemies.Scripts
{
    public interface IEnemyDamageSystem
    {
        void Execute();
        void Initialize();
        void Tick();
    }
}