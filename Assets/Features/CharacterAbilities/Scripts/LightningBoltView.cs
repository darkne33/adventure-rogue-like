using UnityEngine;

// All visual objects and renderers are authored in LightningBolt.prefab.
// The ability advances this view using its own clock, including pause and cleanup.
[DisallowMultipleComponent]
public sealed class LightningBoltView : MonoBehaviour
{
    [SerializeField] private LineRenderer _glow;
    [SerializeField] private LineRenderer _core;
    [SerializeField] private LineRenderer _impact;
    [SerializeField, Min(2)] private int _segments = 12;
    [SerializeField, Min(0f)] private float _jaggedness = 0.25f;
    [SerializeField, Min(0.01f)] private float _flickerInterval = 0.035f;
    [SerializeField, Min(0.01f)] private float _glowWidth = 0.32f;
    [SerializeField, Min(0.01f)] private float _coreWidth = 0.08f;
    [SerializeField, Min(0.01f)] private float _impactRadius = 0.4f;

    private Vector3 _from;
    private Vector3 _to;
    private Vector3 _side;
    private Vector3 _up;
    private float _size;
    private float _lifetime;
    private float _elapsed;
    private int _lastFlickerStep;

    public void Initialize(Vector3 from, Vector3 to, float size, float lifetime)
    {
        _from = from;
        _to = to;
        _size = Mathf.Max(0.01f, size);
        _lifetime = Mathf.Max(0.01f, lifetime);
        _elapsed = 0f;
        _lastFlickerStep = -1;

        Vector3 direction = (to - from).normalized;
        Vector3 reference = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.95f
            ? Vector3.forward
            : Vector3.up;
        _side = Vector3.Cross(direction, reference).normalized;
        _up = Vector3.Cross(direction, _side).normalized;
        if (_glow != null)
            _glow.positionCount = Mathf.Max(2, _segments) + 1;
        if (_core != null)
            _core.positionCount = Mathf.Max(2, _segments) + 1;
        Draw();
    }

    public bool Tick(float deltaTime)
    {
        _elapsed += Mathf.Max(0f, deltaTime);
        if (_elapsed >= _lifetime)
            return false;

        Draw();
        return true;
    }

    private void Draw()
    {
        float progress = Mathf.Clamp01(_elapsed / _lifetime);
        float brightness = (1f - progress) * (1f - progress);
        int flickerStep = Mathf.FloorToInt(_elapsed / Mathf.Max(0.01f, _flickerInterval));
        if (_lastFlickerStep != flickerStep)
        {
            _lastFlickerStep = flickerStep;
            int segmentCount = Mathf.Max(2, _segments);
            float amplitude = Mathf.Min(_jaggedness * _size, Vector3.Distance(_from, _to) * 0.15f);
            float phase = flickerStep * 4.17f + _from.x * 1.73f + _to.z * 2.31f;
            for (int index = 0; index <= segmentCount; index++)
            {
                float fraction = index / (float)segmentCount;
                float envelope = index == 0 || index == segmentCount ? 0f : 1f;
                Vector3 offset = (_side * Mathf.Sin(index * 4.13f + phase) +
                                  _up * Mathf.Sin(index * 7.19f + phase * 1.61f)) * amplitude * envelope;
                Vector3 point = Vector3.Lerp(_from, _to, fraction) + offset;
                if (_glow != null)
                    _glow.SetPosition(index, point);
                if (_core != null)
                    _core.SetPosition(index, point);
            }
        }

        SetAppearance(_glow, _glowWidth * _size, new Color(1f, 0.78f, 0.08f, brightness));
        SetAppearance(_core, _coreWidth * _size, new Color(1f, 1f, 0.9f, brightness));

        if (_impact == null)
            return;

        int points = _impact.positionCount;
        float radius = _impactRadius * _size * (0.5f + progress);
        for (int index = 0; index < points; index++)
        {
            float angle = index * Mathf.PI * 2f / Mathf.Max(1, points);
            _impact.SetPosition(index, _to +
                new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius));
        }
        SetAppearance(_impact, _coreWidth * _size * (1f - progress),
            new Color(1f, 0.88f, 0.2f, brightness));
    }

    private static void SetAppearance(LineRenderer line, float width, Color color)
    {
        if (line == null)
            return;

        line.widthMultiplier = width;
        line.startColor = color;
        line.endColor = color;
    }
}
