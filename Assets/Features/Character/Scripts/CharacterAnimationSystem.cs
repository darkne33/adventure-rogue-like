using UnityEngine;

public class CharacterAnimationSystem
{
    private static readonly int IsMove = Animator.StringToHash("IsMove");
    
    private readonly Animator _animator;

    public CharacterAnimationSystem(Animator animator)
    {
        _animator = animator;
    }
    
    public void MovementPlay(bool isMove) => 
        _animator.SetBool(IsMove, isMove);
}