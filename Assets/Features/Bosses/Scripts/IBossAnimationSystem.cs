namespace Features.Bosses.Scripts
{
    public interface IBossAnimationSystem
    {
        void IdleAnimation();
        void BeginAttack(float warningDuration);
        void SetTimeScale(float timeScale);
    }
}
