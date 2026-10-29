using UnityEngine;

namespace Features.Bosses.Scripts
{
    public sealed class GolemBossAnimation : IBossAnimationSystem
    {
        public float AttackDuration => _attackClip != null ? _attackClip.length : 0f;

        private static readonly int MovementParameter = Animator.StringToHash("Movement");
        private static readonly int AttackParameter = Animator.StringToHash("Attack");
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int MoveState = Animator.StringToHash("Base Layer.Move");
        private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
        private readonly Animator _animator;
        private readonly AnimationClip _attackClip;
        private float _timeScale = 1f;
        private float _attackPlaybackSpeed = 1f;
        private bool _attacking;

        public GolemBossAnimation(Animator animator, AnimationClip attackClip)
        {
            _animator = animator;
            _attackClip = attackClip;
            if (_animator == null)
                return;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        public void IdleAnimation() => SetMovement(false);
        public void MoveAnimation() => SetMovement(true);
        public void BeginAttack(float warningDuration) => StartAttack(1f);

        public void BeginTimedAttack(float duration) => StartAttack(AttackDuration > 0f
            ? AttackDuration / Mathf.Max(0.01f, duration)
            : 1f);

        public void SetAttackWindupProgress(float progress, float warningDuration, float impactTime)
        {
            float playbackSpeed = warningDuration > 0f && impactTime > 0f
                ? impactTime / warningDuration
                : 1f;
            if (!_attacking)
                StartAttack(playbackSpeed);
            else
            {
                _attackPlaybackSpeed = playbackSpeed;
                UpdateSpeed();
            }
        }

        public void SetTimeScale(float timeScale)
        {
            _timeScale = Mathf.Max(0f, timeScale);
            UpdateSpeed();
        }

        private bool CanEvaluate => _animator != null && _animator.isActiveAndEnabled &&
                                    _animator.runtimeAnimatorController != null;

        private void SetMovement(bool moving)
        {
            bool wasAttacking = _attacking;
            _attacking = false;
            _attackPlaybackSpeed = 1f;
            UpdateSpeed();
            if (!CanEvaluate)
                return;
            _animator.SetBool(MovementParameter, moving);
            _animator.ResetTrigger(AttackParameter);
            if (wasAttacking)
                _animator.Play(moving ? MoveState : IdleState, 0, 0f);
        }

        private void StartAttack(float playbackSpeed)
        {
            if (!CanEvaluate)
                return;
            _attacking = true;
            _attackPlaybackSpeed = Mathf.Max(0f, playbackSpeed);
            _animator.SetBool(MovementParameter, false);
            _animator.ResetTrigger(AttackParameter);
            UpdateSpeed();
            _animator.Play(AttackState, 0, 0f);
        }

        private void UpdateSpeed()
        {
            if (_animator != null)
                _animator.speed = _timeScale * (_attacking ? _attackPlaybackSpeed : 1f);
        }
    }
}
