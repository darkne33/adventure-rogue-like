using UnityEngine;

public class CharacterAnimationSystem
{
    private static readonly int IsMove = Animator.StringToHash("IsMove");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int IsGround = Animator.StringToHash("IsGround");

    private readonly Animator _animator;
    private float _speedBeforePause = 1f;
    private bool _isPaused;

    public CharacterAnimationSystem(Animator animator)
    {
        _animator = animator;
    }
    
    public void MovementPlay(bool isMove) => 
        _animator.SetBool(IsMove, isMove);

    public void JumpPlay() => 
        _animator.SetTrigger(Jump);

    public void GroundConditionState(bool state) => 
        _animator.SetBool(IsGround, state);

    public void SetPaused(bool state)
    {
        if (_isPaused == state)
            return;

        _isPaused = state;
        if (state)
        {
            _speedBeforePause = _animator.speed;
            _animator.speed = 0f;
            return;
        }

        _animator.speed = _speedBeforePause;
    }
}
