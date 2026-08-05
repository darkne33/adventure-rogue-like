using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
public sealed class LowHealthVignetteView : MonoBehaviour, IHealthView
{
    [SerializeField, Range(0f, 1f)] private float _healthThreshold = 0.2f;
    [SerializeField] private Color _lowHealthColor = new(0.36f, 0.08f, 0.08f, 1f);
    [SerializeField, Range(0f, 1f)] private float _lowHealthIntensity = 0.4f;
    [SerializeField, Range(0f, 1f)] private float _lowHealthSmoothness = 0.45f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;

    private Vignette _vignette;
    private Color _defaultColor;
    private float _defaultIntensity;
    private float _defaultSmoothness;
    private float _currentBlend;
    private float _targetBlend;
    private bool _isInitialized;

    private void Awake()
    {
        if (TryGetComponent(out Volume volume) == false)
        {
            enabled = false;
            return;
        }

        VolumeProfile profile = volume.profile;

        if (profile == null || profile.TryGet(out _vignette) == false)
        {
            Debug.LogError($"{nameof(LowHealthVignetteView)} requires a Volume Profile with a Vignette override.",
                this);
            enabled = false;
            return;
        }

        _defaultColor = _vignette.color.value;
        _defaultIntensity = _vignette.intensity.value;
        _defaultSmoothness = _vignette.smoothness.value;
        _isInitialized = true;

        ApplyBlend(0f);
    }

    private void Update()
    {
        if (_isInitialized == false || Mathf.Approximately(_currentBlend, _targetBlend))
            return;

        if (_fadeDuration <= 0f)
        {
            _currentBlend = _targetBlend;
        }
        else
        {
            _currentBlend = Mathf.MoveTowards(
                _currentBlend,
                _targetBlend,
                Time.unscaledDeltaTime / _fadeDuration);
        }

        ApplyBlend(_currentBlend);
    }

    public void UpdateHealth(float currentHealth, float maximumHealth)
    {
        float healthRatio = maximumHealth > 0f
            ? Mathf.Clamp01(currentHealth / maximumHealth)
            : 1f;

        _targetBlend = healthRatio < _healthThreshold ? 1f : 0f;

        if (_fadeDuration > 0f || _isInitialized == false)
            return;

        _currentBlend = _targetBlend;
        ApplyBlend(_currentBlend);
    }

    private void OnDisable()
    {
        if (_isInitialized == false)
            return;

        _currentBlend = 0f;
        ApplyBlend(_currentBlend);
    }

    private void ApplyBlend(float blend)
    {
        _vignette.color.value = Color.Lerp(_defaultColor, _lowHealthColor, blend);
        _vignette.intensity.value = Mathf.Lerp(_defaultIntensity, _lowHealthIntensity, blend);
        _vignette.smoothness.value = Mathf.Lerp(_defaultSmoothness, _lowHealthSmoothness, blend);
    }
}
