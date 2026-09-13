using UnityEngine;

namespace Features.Bosses.Scripts
{
    public sealed class WoodGuardBossAnimation : IBossAnimationSystem
    {
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        private readonly Animator _animator;
        private readonly AnimationClip _attackClip;
        private int _currentState;
        private float _playbackSpeed = 1f;

        public WoodGuardBossAnimation(Animator animator, AnimationClip attackClip)
        {
            _animator = animator;
            _attackClip = attackClip;
        }

        public void IdleAnimation()
        {
            if (_animator == null || _currentState == IdleState)
                return;
            _currentState = IdleState;
            _playbackSpeed = 1f;
            _animator.speed = 1f;
            _animator.ResetTrigger(AttackTrigger);
            _animator.Play(IdleState, 0, 0f);
        }

        public void BeginAttack(float warningDuration)
        {
            if (_animator == null)
                return;
            _currentState = AttackState;
            _playbackSpeed = _attackClip != null
                ? _attackClip.length / Mathf.Max(0.01f, warningDuration) : 1f;
            _animator.speed = _playbackSpeed;
            _animator.SetTrigger(AttackTrigger);
        }

        public void SetTimeScale(float timeScale)
        {
            if (_animator != null)
                _animator.speed = _playbackSpeed * Mathf.Max(0f, timeScale);
        }

    }
}
