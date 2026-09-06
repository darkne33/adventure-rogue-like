using UnityEngine;

public class BunEnemyAnimation : IEnemyAnimationSystem
{
    private enum State
    {
        None,
        Idle,
        Run,
        Dash
    }

    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
    
    private readonly Animator _animator;
    private State _state;
    
    public BunEnemyAnimation(Animator animator)
    {
        _animator = animator;
    }
    
    public void IdleAnimation()
    {
        if (_state == State.Idle)
            return;
        _state = State.Idle;
        _animator.SetBool(Attack, false);
        _animator.SetBool(IsRunning, false);
    }

    public void RunAnimation()
    {
        if (_state == State.Run)
            return;
        _state = State.Run;
        _animator.SetBool(Attack, false);
        _animator.SetBool(IsRunning, true);
    }

    public void AttackAnimation()
    {
        if (_state == State.Dash)
            return;
        _state = State.Dash;
        _animator.SetBool(IsRunning, false);
        _animator.SetBool(Attack, true);
        _animator.Play(AttackState, 0, 0f);
    }
}
