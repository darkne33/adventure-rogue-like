using Features.Enemies.Scripts;

public interface ICharacterAimTargetProvider
{
    float TargetingDistance { get; }
    EnemyFacade GetAimedEnemy();
}
