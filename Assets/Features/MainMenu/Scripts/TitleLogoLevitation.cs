using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TitleLogoLevitation : MonoBehaviour
{
    [SerializeField] private RectTransform _logo;
    [SerializeField, Min(0f)] private float _amplitude = 8f;
    [SerializeField, Min(0.01f)] private float _cycleDuration = 3f;

    private Vector2 _initialPosition;
    private Tween _levitationTween;

    private void Awake()
    {
        if (_logo != null)
            _initialPosition = _logo.anchoredPosition;
    }

    private void OnEnable()
    {
        if (_logo == null)
            return;

        _levitationTween = DOVirtual.Float(0f, Mathf.PI * 2f, _cycleDuration,
                phase => _logo.anchoredPosition = _initialPosition +
                    Vector2.up * (Mathf.Sin(phase) * _amplitude))
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void StopAndReset()
    {
        _levitationTween?.Kill();
        _levitationTween = null;

        if (_logo != null)
            _logo.anchoredPosition = _initialPosition;
    }

    private void OnDisable() => StopAndReset();

    private void OnDestroy() => StopAndReset();
}
