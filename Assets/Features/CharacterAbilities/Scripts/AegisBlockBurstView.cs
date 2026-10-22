using System.Collections.Generic;
using UnityEngine;

// Mesh fragments and light streaks are authored in the block prefab.
// The owning ability supplies progress, including its pause/time scaling.
[DisallowMultipleComponent]
public sealed class AegisBlockBurstView : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] _fragments;
    [SerializeField] private MeshRenderer[] _streaks;

    private BurstParticle[] _particles;
    private float _radius;
    private float _halfArc;

    public void Configure(float radius, float arcDegrees)
    {
        _radius = Mathf.Max(0.1f, radius);
        _halfArc = Mathf.Clamp(arcDegrees, 1f, 360f) * 0.5f * Mathf.Deg2Rad;
        if (_particles == null)
        {
            List<BurstParticle> particles = new();
            AddParticles(_fragments, false, particles);
            AddParticles(_streaks, true, particles);
            _particles = particles.ToArray();
        }
        SetProgress(0f);
    }

    public void SetProgress(float progress)
    {
        if (_particles == null)
            return;
        float elapsed = Mathf.Clamp01(progress);
        foreach (BurstParticle particle in _particles)
            particle.SetProgress(elapsed, _radius, _halfArc);
    }

    private static void AddParticles(MeshRenderer[] renderers, bool streak,
        List<BurstParticle> particles)
    {
        if (renderers == null)
            return;
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer != null)
                particles.Add(new BurstParticle(renderer, streak, particles.Count));
        }
    }

    private sealed class BurstParticle
    {
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private readonly MeshRenderer _renderer;
        private readonly Transform _transform;
        private readonly Vector3 _authoredPosition;
        private readonly Quaternion _authoredRotation;
        private readonly Vector3 _authoredScale;
        private readonly MaterialPropertyBlock _properties = new();
        private readonly bool _streak;
        private readonly float _spin;
        private readonly float _distanceFactor;
        private readonly float _angleFactor;

        public BurstParticle(MeshRenderer renderer, bool streak, int index)
        {
            _renderer = renderer;
            _transform = renderer.transform;
            _authoredPosition = _transform.localPosition;
            _authoredRotation = _transform.localRotation;
            _authoredScale = _transform.localScale;
            _streak = streak;
            _spin = (index % 2 == 0 ? 1f : -1f) * (110f + index * 13f);
            _distanceFactor = Mathf.Clamp01(new Vector2(
                _authoredPosition.x, _authoredPosition.z).magnitude);
            // The authored fan spans 120 degrees; respect the actual ability
            // angle without changing any of its damage or collision queries.
            _angleFactor = Mathf.Atan2(_authoredPosition.x, _authoredPosition.z) /
                           (60f * Mathf.Deg2Rad);
            renderer.GetPropertyBlock(_properties);
        }

        public void SetProgress(float progress, float radius, float halfArc)
        {
            float travel = 1f - (1f - progress) * (1f - progress);
            float angle = _angleFactor * halfArc;
            float distance = radius * _distanceFactor * Mathf.Lerp(0.06f, 1f, travel);
            float height = _authoredPosition.y +
                           Mathf.Sin(progress * Mathf.PI) * (_streak ? 0.08f : 0.25f);
            _transform.localPosition = new Vector3(
                Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance);
            _transform.localRotation = _streak
                ? Quaternion.Euler(90f, angle * Mathf.Rad2Deg, 0f)
                : _authoredRotation * Quaternion.Euler(_spin * progress, _spin * progress, 0f);
            float size = _streak ? 1f - progress * 0.65f : 1f - progress * 0.9f;
            _transform.localScale = _authoredScale * Mathf.Max(0f, size);
            _properties.SetFloat(OpacityId, (1f - progress) * (1f - progress));
            _renderer.SetPropertyBlock(_properties);
        }
    }
}
