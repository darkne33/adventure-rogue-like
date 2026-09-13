using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Features.Bosses.UI
{
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public sealed class BossHealthCanvas : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _bossName;
        [SerializeField] private UnityEngine.UI.Image _healthFill;
        [SerializeField] private UnityEngine.UI.Image _damageFill;
        [SerializeField, Min(0f)] private float _damageTrailDelay = 0.3f;
        [SerializeField, Min(0.01f)] private float _damageTrailSpeed = 0.65f;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;

        private float _healthFraction;
        private float _trailStartsAt;

        public void Show(string bossName, float currentHealth, float maxHealth)
        {
            _bossName.text = bossName;
            _healthFraction = GetFraction(currentHealth, maxHealth);
            _healthFill.fillAmount = _healthFraction;
            _damageFill.fillAmount = _healthFraction;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
        }

        public void SetHealth(float currentHealth, float maxHealth)
        {
            float fraction = GetFraction(currentHealth, maxHealth);
            if (Mathf.Approximately(fraction, _healthFraction))
                return;

            if (fraction < _healthFraction)
                _trailStartsAt = Time.unscaledTime + _damageTrailDelay;
            else
                _damageFill.fillAmount = fraction;

            _healthFraction = fraction;
            _healthFill.fillAmount = fraction;
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
            if (Time.unscaledTime < _trailStartsAt || _damageFill.fillAmount <= _healthFraction)
                return;

            _damageFill.fillAmount = Mathf.MoveTowards(_damageFill.fillAmount,
                _healthFraction, _damageTrailSpeed * Time.unscaledDeltaTime);
        }

        private void OnDestroy() => _canvasGroup.DOKill();

        private static float GetFraction(float currentHealth, float maxHealth) =>
            maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    }
}
