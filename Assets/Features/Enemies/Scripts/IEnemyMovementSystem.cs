namespace Features.Enemies.Scripts
{
    public interface IEnemyMovementSystem
    {
        bool CanAttack { get; }

        void Tick();
        void Reset();
    }
}
