using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Features.Bosses.UI
{
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public sealed class BossHealthCanvas : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private UnityEngine.UI.Image _healthFill;
        [SerializeField] private UnityEngine.UI.Image _damageFill;
        [SerializeField, Min(0f)] private float _damageTrailDelay = 0.3f;
        [SerializeField, Min(0.01f)] private float _damageTrailSpeed = 0.65f;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;

        private float _healthFraction;
        private float _damageFraction;
        private float _trailStartsAt;

        public void Show(float currentHealth, float maxHealth)
        {
            UpdateHealthText(currentHealth, maxHealth);
            _healthFraction = GetFraction(currentHealth, maxHealth);
            _damageFraction = _healthFraction;
            SetFill(_healthFill, _healthFraction);
            SetFill(_damageFill, _damageFraction);
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
        }

        public void SetHealth(float currentHealth, float maxHealth)
        {
            UpdateHealthText(currentHealth, maxHealth);
            float fraction = GetFraction(currentHealth, maxHealth);
            if (Mathf.Approximately(fraction, _healthFraction))
                return;

            if (fraction < _healthFraction)
                _trailStartsAt = Time.unscaledTime + _damageTrailDelay;
            else
            {
                _damageFraction = fraction;
                SetFill(_damageFill, _damageFraction);
            }

            _healthFraction = fraction;
            SetFill(_healthFill, fraction);
        }

        public void HideAndDestroy()
        {
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(0f, _fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => Destroy(gameObject));
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < _trailStartsAt || _damageFraction <= _healthFraction)
                return;

            _damageFraction = Mathf.MoveTowards(_damageFraction,
                _healthFraction, _damageTrailSpeed * Time.unscaledDeltaTime);
            SetFill(_damageFill, _damageFraction);
        }

        private void UpdateHealthText(float currentHealth, float maxHealth)
        {
            if (_healthText != null)
                _healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }

        private static void SetFill(UnityEngine.UI.Image image, float fraction)
        {
            // Resize sliced sprites like the character's Slider, preserving their rounded ends.
            Vector2 anchorMax = image.rectTransform.anchorMax;
            anchorMax.x = fraction;
            image.rectTransform.anchorMax = anchorMax;
            image.enabled = fraction > 0f;
        }

        private void OnDestroy() => _canvasGroup.DOKill();

        private static float GetFraction(float currentHealth, float maxHealth) =>
            maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    }
}
