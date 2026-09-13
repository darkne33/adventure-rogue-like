namespace Features.Bosses.Scripts
{
    public interface IBossAnimationSystem
    {
        float AttackDuration { get; }
        void IdleAnimation();
        void BeginAttack(float warningDuration);
        void SetAttackWindupProgress(float progress, float warningDuration, float impactTime);
        void SetTimeScale(float timeScale);
    }
}
