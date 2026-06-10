using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.Enemies.Scripts
{
    public class EnemyHealthView : MonoBehaviour, IHealthView
    {
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private float _visibleDuration = 1f;
        [SerializeField] private float _showDuration = 0.16f;
        [SerializeField] private float _hideDuration = 0.24f;
        [SerializeField] private float _healthChangeDuration = 0.28f;
        [SerializeField] private Color _damageFlashColor = new(1f, 0.75f, 0.45f, 1f);

        [Inject] private DiContainer _container;

        private CanvasGroup _canvasGroup;
        private RectTransform _animationRoot;
        private Image _fillImage;
        private Sequence _visibilitySequence;
        private Sequence _damageSequence;
        private Color _defaultFillColor;
        private float _previousHealth;
        private bool _isInitialized;

        private void Awake()
        {
            InitializeViewToCamera();

            _animationRoot = _healthSlider.transform as RectTransform;
            _fillImage = _healthSlider.fillRect.GetComponent<Image>();
            _defaultFillColor = _fillImage.color;
            _canvasGroup = _healthSlider.GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = _healthSlider.gameObject.AddComponent<CanvasGroup>();

            HideImmediately();
        }

        private void InitializeViewToCamera()
        {
            Canvas canvas = _healthSlider.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            ViewToCamera viewToCamera = canvas.GetComponent<ViewToCamera>();
            if (viewToCamera == null)
                viewToCamera = canvas.gameObject.AddComponent<ViewToCamera>();

            viewToCamera.Initialize((RectTransform)canvas.transform, false);
            _container.Inject(viewToCamera);
        }

        public void UpdateHealth(float currentHealth, float maximumHealth)
        {
            _healthSlider.maxValue = maximumHealth;

            if (!_isInitialized)
            {
                _healthSlider.value = currentHealth;
                _previousHealth = currentHealth;
                _isInitialized = true;
                return;
            }

            if (currentHealth < _previousHealth)
            {
                AnimateDamage(currentHealth);
                ShowTemporarily();
            }
            else
            {
                _healthSlider.value = currentHealth;
            }

            _previousHealth = currentHealth;
        }

        private void AnimateDamage(float targetHealth)
        {
            _damageSequence?.Kill();
            _animationRoot.DOKill();
            _fillImage.DOKill();

            _damageSequence = DOTween.Sequence()
                .SetLink(gameObject)
                .Append(_healthSlider.DOValue(targetHealth, _healthChangeDuration).SetEase(Ease.OutCubic))
                .Join(_animationRoot.DOPunchScale(Vector3.one * 0.16f, 0.3f, 7, 0.55f))
                .Join(_fillImage.DOColor(_damageFlashColor, 0.07f))
                .Append(_fillImage.DOColor(_defaultFillColor, 0.18f).SetEase(Ease.OutQuad));
        }

        private void ShowTemporarily()
        {
            _visibilitySequence?.Kill();

            _visibilitySequence = DOTween.Sequence()
                .SetLink(gameObject)
                .Append(_canvasGroup.DOFade(1f, _showDuration).SetEase(Ease.OutQuad))
                .AppendInterval(_visibleDuration)
                .Append(_canvasGroup.DOFade(0f, _hideDuration).SetEase(Ease.InQuad));
        }

        private void HideImmediately()
        {
            _canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            _visibilitySequence?.Kill();
            _damageSequence?.Kill();
        }
    }
}
