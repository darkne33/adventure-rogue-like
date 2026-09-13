using UnityEngine;

namespace Features.Bosses.Scripts
{
    public sealed class WoodGuardBossAnimation : IBossAnimationSystem
    {
        public float AttackDuration => _attackClip != null ? _attackClip.length : 0f;

        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        private readonly Animator _animator;
        private readonly AnimationClip _attackClip;
        private readonly float _speedBlendDuration;
        private int _currentState;
        private float _playbackSpeed = 1f;
        private float _targetPlaybackSpeed = 1f;
        private float _playbackSpeedVelocity;
        private int _lastSpeedBlendFrame = -1;
        private bool _synchronizedWindup;
        private float _windupStartSpeed;
        private int _lastWindupSampleFrame = -1;

        public WoodGuardBossAnimation(Animator animator, AnimationClip attackClip, float speedBlendDuration)
        {
            _animator = animator;
            _attackClip = attackClip;
            _speedBlendDuration = Mathf.Max(0f, speedBlendDuration);
        }

        public void IdleAnimation()
        {
            if (_animator == null || _currentState == IdleState)
                return;
            _synchronizedWindup = false;
            _lastWindupSampleFrame = -1;
            _currentState = IdleState;
            _targetPlaybackSpeed = 1f;
            _animator.ResetTrigger(AttackTrigger);
            _animator.Play(IdleState, 0, 0f);
        }

        public void SetAttackWindupProgress(float progress, float warningDuration, float impactTime)
        {
            if (_animator == null || AttackDuration <= 0f)
                return;

            if (!_synchronizedWindup)
            {
                _synchronizedWindup = true;
                _windupStartSpeed = Mathf.Max(0f, _playbackSpeed);
                _playbackSpeedVelocity = 0f;
                _currentState = AttackState;
                _targetPlaybackSpeed = 1f;
                _animator.ResetTrigger(AttackTrigger);
            }

            impactTime = Mathf.Clamp(impactTime, 0.01f, AttackDuration);
            float averageSpeed = impactTime / Mathf.Max(0.01f, warningDuration);
            float startSlope = Mathf.Clamp(_windupStartSpeed / averageSpeed, 0f, 3f);
            float endSlope = Mathf.Clamp(1f / averageSpeed, 0f, 3f);
            float t = Mathf.Clamp01(progress);
            float t2 = t * t;
            float t3 = t2 * t;

            // Hermite timing blends speed while reaching the impact frame exactly at warning end.
            float clipProgress = (-2f * t3 + 3f * t2) +
                                 (t3 - 2f * t2 + t) * startSlope + (t3 - t2) * endSlope;
            float speed = (-6f * t2 + 6f * t) +
                          (3f * t2 - 4f * t + 1f) * startSlope + (3f * t2 - 2f * t) * endSlope;
            _playbackSpeed = Mathf.Max(0f, averageSpeed * speed);
            _animator.Play(AttackState, 0, Mathf.Clamp01(clipProgress) * impactTime / AttackDuration);
            _animator.speed = 0f;
            _lastWindupSampleFrame = Time.frameCount;
            if (t >= 1f)
                _synchronizedWindup = false;
        }

        public void BeginAttack(float warningDuration)
        {
            if (_animator == null)
                return;
            _synchronizedWindup = false;
            _lastWindupSampleFrame = -1;
            _currentState = AttackState;
            _targetPlaybackSpeed = _attackClip != null
                ? _attackClip.length / Mathf.Max(0.01f, warningDuration) : 1f;
            _animator.SetTrigger(AttackTrigger);
        }

        public void SetTimeScale(float timeScale)
        {
            if (_animator == null)
                return;

            if (_synchronizedWindup || _lastWindupSampleFrame == Time.frameCount)
            {
                // The same clock drives the warning, the sampled pose and root emergence.
                _animator.speed = 0f;
                return;
            }

            timeScale = Mathf.Max(0f, timeScale);
            float deltaTime = Time.deltaTime * timeScale;
            if (_speedBlendDuration <= 0f)
            {
                _playbackSpeed = _targetPlaybackSpeed;
                _playbackSpeedVelocity = 0f;
            }
            else if (_lastSpeedBlendFrame != Time.frameCount && deltaTime > 0f)
            {
                // Attack sequences can update the time scale several times in one frame.
                _playbackSpeed = Mathf.SmoothDamp(_playbackSpeed, _targetPlaybackSpeed,
                    ref _playbackSpeedVelocity, _speedBlendDuration, Mathf.Infinity,
                    deltaTime);
                _lastSpeedBlendFrame = Time.frameCount;
            }

            // Gameplay freezes still stop the animator immediately.
            _animator.speed = _playbackSpeed * timeScale;
        }

    }
}
