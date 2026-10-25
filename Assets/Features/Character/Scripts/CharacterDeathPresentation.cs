using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class CharacterDeathPresentation : MonoBehaviour
{
    [SerializeField] private CharacterDeathConfiguration _configuration;
    [SerializeField] private Transform _bodyPivot;
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
        if (_configuration == null || _bodyPivot == null || _animator == null || _soulOrigin == null)
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
            // The Animator stays disabled for the corpse's remaining lifetime.
            _animator.enabled = false;
            foreach (ParticleSystem effect in _bodyEffects)
                if (effect != null)
                    effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Vector3 startPosition = _bodyPivot.position;
            Quaternion startRotation = _bodyPivot.rotation;
            Vector3 right = Vector3.ProjectOnPlane(_character.CameraPivot.right, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.right;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, right).normalized;
            Vector3 landingPosition = startPosition;
            landingPosition.y += groundHeight - _character.transform.position.y;
            float bodyHeight = TryGetBodyBounds(out Bounds initialBounds)
                ? Mathf.Max(0.1f, initialBounds.size.y) : 1f;
            float unitsPerPixel = bodyHeight / _configuration.PixelsPerBodyHeight;
            float elapsed = 0f;
            bool landed = false;
            bool soulStarted = false;

            while (elapsed < _configuration.Duration)
            {
                token.ThrowIfCancellationRequested();
                if (!landed)
                {
                    float progress = Mathf.Clamp01(elapsed / _configuration.FallDuration);
                    SampleFall(progress, groundHeight, startPosition, landingPosition,
                        startRotation, rotationAxis);
                    landed = progress >= 1f;
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
                    Vector3 offset = (right * pose.HorizontalPixels + Vector3.up * pose.RisePixels) * unitsPerPixel;
                    _soulView.Sample(soulElapsed, offset, pose);
                }

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token);
                elapsed += Mathf.Max(0f, Time.unscaledDeltaTime);
            }

            token.ThrowIfCancellationRequested();
            if (!landed)
                SampleFall(1f, groundHeight, startPosition, landingPosition, startRotation, rotationAxis);
        }
        finally
        {
            _soulView.Stop();
            _playCancellation = null;
        }
    }

    private void SampleFall(float progress, float groundHeight, Vector3 startPosition,
        Vector3 landingPosition, Quaternion startRotation, Vector3 rotationAxis)
    {
        float eased = Mathf.SmoothStep(0f, 1f, progress);
        _bodyPivot.SetPositionAndRotation(
            Vector3.Lerp(startPosition, landingPosition, eased),
            Quaternion.AngleAxis(_configuration.FallAngle * eased, rotationAxis) * startRotation);

        if (!TryGetBodyBounds(out Bounds bounds))
            return;

        float correction = groundHeight + _configuration.BodyGroundClearance * eased - bounds.min.y;
        if (correction > 0f || progress >= 1f)
            _bodyPivot.position += Vector3.up * correction;
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
