using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class CharacterDeathPresentation : MonoBehaviour
{
    private static readonly int JumpPoseState = Animator.StringToHash("Base Layer.Jump");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int IsGround = Animator.StringToHash("IsGround");
    private static readonly int IsMove = Animator.StringToHash("IsMove");

    [SerializeField] private CharacterDeathConfiguration _configuration;
    [SerializeField] private Transform _bodyPivot;
    [SerializeField] private Transform _pumpPivot;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _soulOrigin;

    private CharacterFacade _character;
    private CharacterDeathSoulView _soulView;
    private Renderer[] _bodyRenderers;
    private ParticleSystem[] _bodyEffects;
    private CancellationTokenSource _playCancellation;
    private bool _started;

    public CharacterDeathSoulView SoulPrefab => _configuration != null ? _configuration.SoulPrefab : null;

    public void Initialize(CharacterFacade character, CharacterDeathSoulView soulView)
    {
        if (_configuration == null || _bodyPivot == null || _pumpPivot == null ||
            _animator == null || _soulOrigin == null)
            throw new InvalidOperationException($"{name} is missing prepared character death references.");

        _character = character;
        _soulView = soulView != null ? soulView : throw new ArgumentNullException(nameof(soulView));
        _bodyRenderers = character.MeshRenderers;
        _bodyEffects = _bodyPivot.GetComponentsInChildren<ParticleSystem>(true);
        _soulView.Stop();
    }

    public async UniTask Play(float groundHeight, CancellationToken cancellationToken)
    {
        if (_started)
            return;

        _started = true;
        if (_character == null || _soulView == null)
            throw new InvalidOperationException($"{name} death presentation was not initialized.");

        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, destroyCancellationToken);
        _playCancellation = lifetime;
        CancellationToken token = lifetime.Token;

        try
        {
            token.ThrowIfCancellationRequested();
            FreezeJumpPose();
            foreach (ParticleSystem effect in _bodyEffects)
                if (effect != null)
                    effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Vector3 startPosition = _bodyPivot.position;
            Quaternion startRotation = _bodyPivot.rotation;
            Vector3 startPumpScale = _pumpPivot.localScale;
            Vector3 right = Vector3.ProjectOnPlane(_character.CameraPivot.right, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.right;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, right).normalized;
            bool hasInitialBounds = TryGetBodyBounds(out Bounds initialBounds);
            float bodyHeight = hasInitialBounds
                ? Mathf.Max(0.1f, initialBounds.size.y) : 1f;
            float initialGroundGap = hasInitialBounds
                ? Mathf.Max(0f, initialBounds.min.y - groundHeight - _configuration.BodyGroundClearance) : 0f;
            float bodyUnitsPerPixel = bodyHeight / _configuration.BodyMotionPixelsPerHeight;
            float soulUnitsPerPixel = bodyHeight / _configuration.PixelsPerBodyHeight;
            float elapsed = 0f;
            bool bodySettled = false;
            bool soulStarted = false;

            while (elapsed < _configuration.Duration)
            {
                token.ThrowIfCancellationRequested();
                if (!bodySettled)
                {
                    SampleBody(Mathf.Min(elapsed, _configuration.BodyMotionDuration), groundHeight,
                        startPosition, startRotation, startPumpScale, right, rotationAxis,
                        bodyUnitsPerPixel, initialGroundGap);
                    bodySettled = elapsed >= _configuration.BodyMotionDuration;
                }

                if (elapsed >= _configuration.SoulDelay)
                {
                    if (!soulStarted)
                    {
                        Vector3 origin = _soulOrigin.position;
                        origin.y += groundHeight - _character.transform.position.y;
                        if (TryGetBodyBounds(out Bounds corpseBounds))
                        {
                            origin.x = corpseBounds.center.x;
                            origin.z = corpseBounds.center.z;
                        }

                        _soulView.Begin(origin, _configuration.SoulSizeMultiplier);
                        soulStarted = true;
                    }

                    float soulElapsed = elapsed - _configuration.SoulDelay;
                    CharacterDeathConfiguration.SoulPose pose = _configuration.SampleSoul(soulElapsed);
                    Vector3 offset = (right * pose.HorizontalPixels + Vector3.up * pose.RisePixels) * soulUnitsPerPixel;
                    _soulView.Sample(soulElapsed, offset, pose);
                }

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token);
                elapsed += Mathf.Max(0f, Time.unscaledDeltaTime);
            }

            token.ThrowIfCancellationRequested();
            if (!bodySettled)
                SampleBody(_configuration.BodyMotionDuration, groundHeight, startPosition,
                    startRotation, startPumpScale, right, rotationAxis, bodyUnitsPerPixel, initialGroundGap);
        }
        finally
        {
            _soulView.Stop();
            _playCancellation = null;
        }
    }

    private void FreezeJumpPose()
    {
        // Avoid Jump's immediate grounded exit and animation events before freezing frame zero.
        _animator.enabled = true;
        _animator.fireEvents = false;
        _animator.speed = 1f;
        _animator.SetBool(IsGround, false);
        _animator.SetBool(IsMove, false);
        _animator.ResetTrigger(Jump);
        _animator.Play(JumpPoseState, 0, 0f);
        _animator.Update(0f);
        _animator.enabled = false;
    }

    private void SampleBody(float elapsed, float groundHeight, Vector3 startPosition,
        Quaternion startRotation, Vector3 startPumpScale, Vector3 right, Vector3 rotationAxis,
        float unitsPerPixel, float initialGroundGap)
    {
        float progress = Mathf.Clamp01((elapsed - _configuration.LaunchDelay) / _configuration.FallDuration);
        float travelProgress = Mathf.SmoothStep(0f, 1f, progress);
        float rotationProgress = Mathf.InverseLerp(
            _configuration.LaunchDelay + _configuration.FallRotationDelay,
            _configuration.LandingTime, elapsed);
        float easedRotation = Mathf.SmoothStep(0f, 1f, rotationProgress);
        Vector2 pump = _configuration.SamplePump(elapsed);
        float shakePixels = _configuration.SampleShake(elapsed - _configuration.LandingTime);
        float horizontalPixels = _configuration.BodyTravelPixels * travelProgress + shakePixels;

        // The prepared outer pivot keeps squash/stretch vertical as the inner model falls sideways.
        _pumpPivot.localScale = Vector3.Scale(startPumpScale, new Vector3(pump.x, pump.y, pump.x));
        _bodyPivot.SetPositionAndRotation(
            startPosition + right * (horizontalPixels * unitsPerPixel),
            Quaternion.AngleAxis(_configuration.FallAngle * easedRotation, rotationAxis) * startRotation);

        if (!TryGetBodyBounds(out Bounds bounds))
            return;

        float hopHeight = _configuration.HopHeightPixels * unitsPerPixel * 4f * progress * (1f - progress);
        float remainingGroundGap = initialGroundGap * (1f - travelProgress);
        float targetBottom = groundHeight + _configuration.BodyGroundClearance + hopHeight + remainingGroundGap;
        _bodyPivot.position += Vector3.up * (targetBottom - bounds.min.y);
    }

    private bool TryGetBodyBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Renderer renderer in _bodyRenderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.transform.IsChildOf(_bodyPivot))
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
                bounds.Encapsulate(renderer.bounds);
        }

        return found;
    }

    private void OnDisable() => _playCancellation?.Cancel();
}
