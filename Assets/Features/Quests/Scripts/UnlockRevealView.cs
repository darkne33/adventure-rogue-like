using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Quests.Scripts
{
    public sealed class UnlockRevealView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _card;
        [SerializeField] private RectTransform _iconRoot;
        [SerializeField] private RectTransform _rays;
        [SerializeField] private UnityEngine.UI.Image _icon;
        [SerializeField] private TMP_Text _category;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private UnityEngine.UI.Button _continueButton;
        [SerializeField, Min(0.01f)] private float _appearanceDuration = 0.42f;
        [SerializeField, Min(0f)] private float _inputDelay = 0.18f;
        [SerializeField, Min(0.01f)] private float _closeDuration = 0.14f;
        [SerializeField] private float _rayRotationSpeed = 8f;

        private Tween _appearance;
        private Tween _closing;
        private bool _ready;

        public UnityEngine.UI.Image Icon => _icon;
        public bool IsVisible { get; private set; }
        public event Action CardRevealed;
        public event Action Dismissed;

        private void Awake() => _continueButton.onClick.AddListener(Dismiss);

        public void Show(UnlockDefinition unlock)
        {
            StopAnimations();
            _category.text = unlock.Category switch
            {
                ProgressionCategory.Characters => "NEW CHARACTER",
                ProgressionCategory.Weapons => "NEW WEAPON",
                ProgressionCategory.Scrolls => "NEW SCROLL",
                ProgressionCategory.Relics => "NEW RELIC",
                _ => "NEW UNLOCK"
            };
            _name.text = unlock.DisplayName.ToUpperInvariant();
            _description.text = unlock.Description;
            _ready = false;
            IsVisible = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;
            _continueButton.interactable = false;
            _card.localScale = Vector3.one * 0.78f;
            _iconRoot.localScale = Vector3.one * 0.4f;
            _rays.localRotation = Quaternion.identity;
            EventSystem.current?.SetSelectedGameObject(null);

            _appearance = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(1f, _appearanceDuration * 0.55f))
                .Insert(0f, _card.DOScale(Vector3.one, _appearanceDuration).SetEase(Ease.OutBack)
                    .OnComplete(() => CardRevealed?.Invoke()))
                .Insert(0.08f, _iconRoot.DOScale(Vector3.one, _appearanceDuration).SetEase(Ease.OutBack))
                .AppendInterval(_inputDelay)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _appearance = null;
                    _ready = true;
                    _canvasGroup.interactable = true;
                    _continueButton.interactable = true;
                    ProgressionMenuUi.Focus(_continueButton, false);
                });
        }

        public void Dismiss()
        {
            if (!IsVisible || !_ready)
                return;

            _ready = false;
            _canvasGroup.interactable = false;
            _continueButton.interactable = false;
            _closing = _canvasGroup.DOFade(0f, _closeDuration)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _closing = null;
                    HideImmediate();
                    Dismissed?.Invoke();
                });
        }

        public void HideImmediate()
        {
            StopAnimations();
            IsVisible = false;
            _ready = false;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsVisible)
                return;

            _rays.Rotate(Vector3.forward, _rayRotationSpeed * Time.unscaledDeltaTime);
            EventSystem eventSystem = EventSystem.current;
            if (_ready && eventSystem != null &&
                eventSystem.currentSelectedGameObject != _continueButton.gameObject)
                ProgressionMenuUi.Focus(_continueButton, false);
        }

        private void StopAnimations()
        {
            _appearance?.Kill();
            _closing?.Kill();
            _appearance = null;
            _closing = null;
        }

        private void OnDisable()
        {
            StopAnimations();
            IsVisible = false;
            _ready = false;
        }

        private void OnDestroy()
        {
            StopAnimations();
            _continueButton.onClick.RemoveListener(Dismiss);
        }
    }
}
