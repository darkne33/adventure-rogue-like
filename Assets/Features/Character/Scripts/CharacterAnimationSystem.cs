using UnityEngine;

public class CharacterAnimationSystem
{
    private static readonly int IsMove = Animator.StringToHash("IsMove");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int IsGround = Animator.StringToHash("IsGround");

    private readonly Animator _animator;

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
}