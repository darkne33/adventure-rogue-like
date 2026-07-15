using UnityEngine;

public class CharacterAnimationSystem
{
    private static readonly int IsMove = Animator.StringToHash("IsMove");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int IsGround = Animator.StringToHash("IsGround");
    private static readonly int StartChestOpeningTrigger = Animator.StringToHash("StartChestOpening");
    private static readonly int FinishChestOpeningTrigger = Animator.StringToHash("FinishChestOpening");
    private static readonly int EndChestOpeningTrigger = Animator.StringToHash("EndChestOpening");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");

    private const float ChestOpeningExitTransitionDuration = 0.1f;

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

    public void StartChestOpening()
    {
        _animator.ResetTrigger(FinishChestOpeningTrigger);
        _animator.ResetTrigger(EndChestOpeningTrigger);
        _animator.SetBool(IsMove, false);
        _animator.SetTrigger(StartChestOpeningTrigger);
    }

    public void FinishChestOpeningAnimation()
    {
        _animator.ResetTrigger(StartChestOpeningTrigger);
        _animator.ResetTrigger(EndChestOpeningTrigger);
        _animator.SetTrigger(FinishChestOpeningTrigger);
    }

    public void EndChestOpening()
    {
        _animator.ResetTrigger(StartChestOpeningTrigger);
        _animator.ResetTrigger(FinishChestOpeningTrigger);
        _animator.SetTrigger(EndChestOpeningTrigger);
    }

    public void ResetChestOpening()
    {
        _animator.ResetTrigger(StartChestOpeningTrigger);
        _animator.ResetTrigger(FinishChestOpeningTrigger);
        _animator.ResetTrigger(EndChestOpeningTrigger);
        _animator.CrossFade(IdleState, ChestOpeningExitTransitionDuration);
    }

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
