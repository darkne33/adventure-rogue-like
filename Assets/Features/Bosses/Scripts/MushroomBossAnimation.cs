using UnityEngine;

namespace Features.Bosses.Scripts
{
    public sealed class MushroomBossAnimation : IBossAnimationSystem
    {
        public float AttackDuration => _selectedClip != null ? _selectedClip.length : 0f;

        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int JumpState = Animator.StringToHash("Base Layer.Attack_Jump");
        private static readonly int HeadState = Animator.StringToHash("Base Layer.Attack_Head");
        private readonly Animator _animator;
        private readonly AnimationClip _jumpClip;
        private readonly AnimationClip _headClip;
        private AnimationClip _selectedClip;
        private int _currentState;
        private float _timeScale = 1f;
        private bool _samplingAttack;
        private bool _evaluatingPose;

        public MushroomBossAnimation(Animator animator, AnimationClip jumpClip, AnimationClip headClip)
        {
            _animator = animator;
            _jumpClip = jumpClip;
            _headClip = headClip;
            _selectedClip = jumpClip;
            if (_animator == null)
                return;

            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        public void BeginJump() => BeginSampledAttack(JumpState, _jumpClip);

        public void BeginHead() => BeginSampledAttack(HeadState, _headClip);

        public void SampleAttack(float normalizedTime)
        {
            if (!_samplingAttack || !CanEvaluate || _evaluatingPose)
                return;

            // Movement, warning growth and impact use this same normalized attack clock.
            _animator.speed = 0f;
            _animator.Play(_currentState, 0, Mathf.Clamp01(normalizedTime));
            _evaluatingPose = true;
            try
            {
                _animator.Update(0f);
            }
            finally
            {
                _evaluatingPose = false;
            }
        }

        public void IdleAnimation()
        {
            _samplingAttack = false;
            if (_animator != null)
                _animator.speed = _timeScale;
            if (_currentState == IdleState || !CanEvaluate)
                return;

            _currentState = IdleState;
            _animator.Play(IdleState, 0, 0f);
        }

        public void SetTimeScale(float timeScale)
        {
            _timeScale = Mathf.Max(0f, timeScale);
            if (_animator != null)
                _animator.speed = _samplingAttack ? 0f : _timeScale;
        }

        public void BeginAttack(float warningDuration) => BeginJump();

        public void SetAttackWindupProgress(float progress, float warningDuration, float impactTime)
        {
            if (!_samplingAttack)
                BeginJump();
            float impactNormalized = AttackDuration > 0f ? Mathf.Clamp01(impactTime / AttackDuration) : 1f;
            SampleAttack(Mathf.Clamp01(progress) * impactNormalized);
        }

        private bool CanEvaluate => _animator != null && _animator.isActiveAndEnabled &&
                                    _animator.runtimeAnimatorController != null;

        private void BeginSampledAttack(int state, AnimationClip clip)
        {
            _currentState = state;
            _selectedClip = clip;
            _samplingAttack = true;
            SampleAttack(0f);
        }
    }
}
