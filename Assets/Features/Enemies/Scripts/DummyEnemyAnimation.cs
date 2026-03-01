using UnityEngine;

public class DummyEnemyAnimation : IEnemyAnimationSystem
{
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int Attack = Animator.StringToHash("Attack");

    private readonly Animator _animator;

    public DummyEnemyAnimation(Animator animator)
    {
        _animator = animator;
    }

    public void IdleAnimation()
    {
    }

    public void RunAnimation() =>
        _animator.SetBool(IsRunning, true);

    public void AttackAnimation()
    {
        _animator.SetBool(IsRunning, false);
        _animator.SetTrigger(Attack);
    }
}