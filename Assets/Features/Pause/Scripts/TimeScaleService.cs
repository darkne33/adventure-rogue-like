using System.Collections.Generic;
using UnityEngine;

public sealed class TimeScaleService : ITimeScaleService
{
    private const float MinimumTimeScale = 0.01f;

    private readonly HashSet<TimeScaleRequest> _requests = new();

    private float _baseTimeScale;

    public bool IsPaused { get; private set; }

    public TimeScaleService()
    {
        _baseTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
    }

    public ITimeScaleRequest Request(float timeScale)
    {
        CaptureBaseTimeScaleIfIdle();

        TimeScaleRequest request = new(this, ClampTimeScale(timeScale));
        _requests.Add(request);
        ApplyTimeScale();
        return request;
    }

    public void SetPaused(bool isPaused)
    {
        if (IsPaused == isPaused)
            return;

        if (isPaused)
            CaptureBaseTimeScaleIfIdle();

        IsPaused = isPaused;
        ApplyTimeScale();
    }

    private void CaptureBaseTimeScaleIfIdle()
    {
        if (_requests.Count == 0 && !IsPaused && Time.timeScale > 0f)
            _baseTimeScale = Time.timeScale;
    }

    private void Update(TimeScaleRequest request, float timeScale)
    {
        if (!_requests.Contains(request))
            return;

        request.SetTimeScale(ClampTimeScale(timeScale));
        ApplyTimeScale();
    }

    private void Release(TimeScaleRequest request)
    {
        if (!_requests.Remove(request))
            return;

        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        if (IsPaused)
        {
            Time.timeScale = 0f;
            return;
        }

        float effectiveTimeScale = _baseTimeScale;
        foreach (TimeScaleRequest request in _requests)
            effectiveTimeScale = Mathf.Min(effectiveTimeScale, request.TimeScale);

        Time.timeScale = effectiveTimeScale;
    }

    private static float ClampTimeScale(float timeScale) =>
        Mathf.Clamp(timeScale, MinimumTimeScale, 1f);

    private sealed class TimeScaleRequest : ITimeScaleRequest
    {
        private TimeScaleService _owner;
        private float _timeScale;

        public float TimeScale
        {
            get => _timeScale;
            set => _owner?.Update(this, value);
        }

        public TimeScaleRequest(TimeScaleService owner, float timeScale)
        {
            _owner = owner;
            _timeScale = timeScale;
        }

        public void Dispose()
        {
            TimeScaleService owner = _owner;
            if (owner == null)
                return;

            _owner = null;
            owner.Release(this);
        }

        public void SetTimeScale(float timeScale) =>
            _timeScale = timeScale;
    }
}
