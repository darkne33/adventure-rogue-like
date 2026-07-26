using UnityEngine;

public class BunEnemyAnimation : IEnemyAnimationSystem
{
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int Attack = Animator.StringToHash("Attack");
    
    private readonly Animator _animator;
    
    public BunEnemyAnimation( Animator animator)
    {
        _animator = animator;
    }
    
    public void IdleAnimation() =>
        _animator.SetBool(IsRunning, false);

    public void RunAnimation()
    {
        _animator.SetBool(IsRunning, true);
    }

    public void AttackAnimation()
    {
       
    }
}
