using TMPro;
using UnityEngine;

namespace Features.Quests.Scripts
{
    public sealed class QuestCompletionNotificationView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _card;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _condition;
        [SerializeField] private TMP_Text _reward;
        [SerializeField] private UnityEngine.UI.Image _icon;

        private Vector2 _restingPosition;
        private bool _initialized;

        private void Awake()
        {
            Initialize();
            Hide();
        }

        public void SetContent(string title, string condition, string reward, Sprite icon)
        {
            if (_title != null)
                _title.text = title ?? string.Empty;
            if (_condition != null)
                _condition.text = condition ?? string.Empty;
            if (_reward != null)
                _reward.text = reward ?? string.Empty;
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.preserveAspect = true;
                _icon.enabled = icon != null;
            }
        }

        public void SetPresentation(float alpha, float verticalOffset)
        {
            Initialize();
            if (_canvasGroup != null)
                _canvasGroup.alpha = Mathf.Clamp01(alpha);
            if (_card != null)
                _card.anchoredPosition = _restingPosition + Vector2.up * verticalOffset;
        }

        public void Hide() => SetPresentation(0f, 0f);

        private void Initialize()
        {
            if (_initialized)
                return;

            if (_card != null)
                _restingPosition = _card.anchoredPosition;
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            _initialized = true;
        }
    }
}
