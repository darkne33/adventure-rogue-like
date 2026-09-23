using UnityEngine;

namespace Features.Quests.Scripts
{
    [DisallowMultipleComponent]
    public sealed class ProgressionAlertPulse : MonoBehaviour
    {
        [SerializeField] private RectTransform _icon;
        [SerializeField, Min(0.1f)] private float _period = 1.2f;
        [SerializeField, Range(0f, 0.3f)] private float _scaleAmount = 0.08f;
        [SerializeField, Range(0f, 20f)] private float _tilt = 6f;

        private Vector3 _restScale;
        private Quaternion _restRotation;
        private float _elapsed;

        private void OnEnable()
        {
            if (_icon == null)
                return;
            _restScale = _icon.localScale;
            _restRotation = _icon.localRotation;
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_icon == null)
                return;
            _elapsed += Time.unscaledDeltaTime;
            float phase = _elapsed * (2f * Mathf.PI / Mathf.Max(0.1f, _period));
            _icon.localScale = _restScale * (1f + _scaleAmount * (0.5f - 0.5f * Mathf.Cos(phase)));
            _icon.localRotation = _restRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * _tilt);
        }

        private void OnDisable()
        {
            if (_icon == null)
                return;
            _icon.localScale = _restScale;
            _icon.localRotation = _restRotation;
        }
    }
}
