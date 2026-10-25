using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Character/Death", fileName = "CharacterDeathConfiguration")]
public sealed class CharacterDeathConfiguration : ScriptableObject
{
    [SerializeField] private CharacterDeathSoulView _soulPrefab;
    [SerializeField, Min(0f)] private float _launchDelay = 4f / 30f;
    [SerializeField, Min(0.01f)] private float _fallDuration = 0.4f;
    [SerializeField, Min(0f)] private float _fallRotationDelay = 0.12f;
    [SerializeField, Min(0f)] private float _hopHeightPixels = 52f;
    [SerializeField, Min(0f)] private float _bodyTravelPixels = 22f;
    [SerializeField, Min(1f)] private float _bodyMotionPixelsPerHeight = 69f;
    [SerializeField, Min(0f)] private float _soulDelay = 22f / 30f;
    [SerializeField, Min(0.01f)] private float _duration = 57f / 30f;
    [SerializeField] private float _fallAngle = 90f;
    [SerializeField, Min(0f)] private float _bodyGroundClearance = 0.08f;
    [SerializeField, Min(1f)] private float _pixelsPerBodyHeight = 32f;
    [SerializeField, Min(0.01f)] private float _soulSizeMultiplier = 3.5f;
    [SerializeField] private PumpPose[] _pumpPoses = Array.Empty<PumpPose>();
    [SerializeField] private ShakePose[] _shakePoses = Array.Empty<ShakePose>();
    [SerializeField] private SoulPose[] _soulPoses = Array.Empty<SoulPose>();

    public CharacterDeathSoulView SoulPrefab => _soulPrefab;
    public float LaunchDelay => Mathf.Max(0f, _launchDelay);
    public float FallDuration => Mathf.Max(0.01f, _fallDuration);
    public float LandingTime => LaunchDelay + FallDuration;
    public float FallRotationDelay => Mathf.Max(0f, _fallRotationDelay);
    public float HopHeightPixels => Mathf.Max(0f, _hopHeightPixels);
    public float BodyTravelPixels => Mathf.Max(0f, _bodyTravelPixels);
    public float BodyMotionPixelsPerHeight => Mathf.Max(1f, _bodyMotionPixelsPerHeight);
    public float BodyMotionDuration
    {
        get
        {
            float pumpDuration = _pumpPoses != null && _pumpPoses.Length > 0
                ? Mathf.Max(0f, _pumpPoses[_pumpPoses.Length - 1].Frame) / 30f
                : 0f;
            float shakeDuration = _shakePoses != null && _shakePoses.Length > 0
                ? Mathf.Max(0f, _shakePoses[_shakePoses.Length - 1].Frame) / 30f
                : 0f;
            return Mathf.Max(LandingTime + shakeDuration, pumpDuration);
        }
    }
    public float SoulDelay => Mathf.Max(BodyMotionDuration, _soulDelay);
    public float Duration => Mathf.Max(SoulDelay + 0.01f, _duration);
    public float FallAngle => _fallAngle;
    public float BodyGroundClearance => Mathf.Max(0f, _bodyGroundClearance);
    public float PixelsPerBodyHeight => Mathf.Max(1f, _pixelsPerBodyHeight);
    public float SoulSizeMultiplier => Mathf.Max(0.01f, _soulSizeMultiplier);

    public Vector2 SamplePump(float elapsed)
    {
        if (_pumpPoses == null || _pumpPoses.Length == 0)
            return Vector2.one;

        float frame = Mathf.Max(0f, elapsed) * 30f;
        PumpPose from = _pumpPoses[0];
        for (int i = 1; i < _pumpPoses.Length; i++)
        {
            PumpPose to = _pumpPoses[i];
            if (frame < to.Frame)
                return Vector2.Lerp(from.Scale, to.Scale,
                    Mathf.InverseLerp(from.Frame, to.Frame, frame));
            from = to;
        }

        return from.Scale;
    }

    public float SampleShake(float elapsedSinceLanding)
    {
        if (elapsedSinceLanding < 0f || _shakePoses == null || _shakePoses.Length == 0)
            return 0f;

        float frame = elapsedSinceLanding * 30f;
        ShakePose from = _shakePoses[0];
        for (int i = 1; i < _shakePoses.Length; i++)
        {
            ShakePose to = _shakePoses[i];
            if (frame < to.Frame)
                return Mathf.Lerp(from.HorizontalPixels, to.HorizontalPixels,
                    Mathf.InverseLerp(from.Frame, to.Frame, frame));
            from = to;
        }

        return from.HorizontalPixels;
    }

    public SoulPose SampleSoul(float elapsed)
    {
        if (_soulPoses == null || _soulPoses.Length == 0)
            return default;

        float frame = Mathf.Max(0f, elapsed) * 30f;
        SoulPose from = _soulPoses[0];
        for (int i = 1; i < _soulPoses.Length; i++)
        {
            SoulPose to = _soulPoses[i];
            if (frame < to.Frame)
                return SoulPose.Lerp(from, to, Mathf.InverseLerp(from.Frame, to.Frame, frame));
            from = to;
        }

        return from;
    }

    [Serializable]
    public struct PumpPose
    {
        public float Frame;
        public Vector2 Scale;
    }

    [Serializable]
    public struct ShakePose
    {
        public float Frame;
        public float HorizontalPixels;
    }

    [Serializable]
    public struct SoulPose
    {
        public float Frame;
        public float HorizontalPixels;
        public float RisePixels;
        [Range(0f, 1f)] public float Opacity;
        public Vector2 Scale;

        internal static SoulPose Lerp(SoulPose from, SoulPose to, float progress) => new()
        {
            Frame = Mathf.Lerp(from.Frame, to.Frame, progress),
            HorizontalPixels = Mathf.Lerp(from.HorizontalPixels, to.HorizontalPixels, progress),
            RisePixels = Mathf.Lerp(from.RisePixels, to.RisePixels, progress),
            Opacity = Mathf.Lerp(from.Opacity, to.Opacity, progress),
            Scale = Vector2.Lerp(from.Scale, to.Scale, progress)
        };
    }
}
