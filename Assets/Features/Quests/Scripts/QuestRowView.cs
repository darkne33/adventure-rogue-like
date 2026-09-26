using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Quests.Scripts
{
    public sealed class QuestRowView : MonoBehaviour, ISelectHandler
    {
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private UnityEngine.UI.Image _background;
        [SerializeField] private UnityEngine.UI.Toggle _completedCheck;
        [SerializeField] private TMP_Text _condition;
        [SerializeField] private GameObject _progressRoot;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private RectTransform _progressFill;
        [SerializeField] private UnityEngine.UI.Image _rewardIcon;
        [SerializeField] private GameObject _rewardAlert;

        private bool _appearanceCached;
        private Color _baseColor;
        private Vector2 _conditionAnchorMax;

        public UnityEngine.UI.Button Button => _button;
        public RectTransform Rect => (RectTransform)transform;
        public QuestDefinition Quest { get; private set; }
        public event Action<QuestRowView> Selected;

        public void Bind(QuestDefinition quest, QuestService service, int index,
            Func<string, Sprite> getPortrait = null, Material portraitMaterial = null)
        {
            if (!_appearanceCached)
            {
                _baseColor = _background.color;
                _conditionAnchorMax = _condition.rectTransform.anchorMax;
                _appearanceCached = true;
            }
            Quest = quest;
            _button.onClick.RemoveListener(RequestSelection);
            _button.onClick.AddListener(RequestSelection);
            _background.color = index % 2 == 0 ? _baseColor : Color.Lerp(_baseColor, Color.white, 0.025f);
            bool completed = service.IsCompleted(quest.Id);
            _rewardAlert.SetActive(service.CanClaimReward(quest.Id));
            _completedCheck.SetIsOnWithoutNotify(service.IsRewardClaimed(quest.Id));
            _condition.text = quest.Description;
            bool showProgress = quest.Target > 1 && !service.IsRewardClaimed(quest.Id);
            _progressRoot.SetActive(showProgress);
            Vector2 conditionRight = _conditionAnchorMax;
            if (!showProgress)
                conditionRight.x = ((RectTransform)_progressRoot.transform).anchorMax.x;
            _condition.rectTransform.anchorMax = conditionRight;
            int progress = Mathf.Clamp(service.GetProgress(quest), 0, quest.Target);
            _progressText.text = $"{progress:N0} / {quest.Target:N0}";
            float fraction = quest.Target > 0 ? (float)progress / quest.Target : 0f;
            _progressFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);

            UnlockDefinition unlock = service.GetUnlockForQuest(quest.Id);
            if (unlock != null)
            {
                ProgressionMenuUi ui = new(service.Configuration, getPortrait, portraitMaterial);
                ui.Icon(_rewardIcon, unlock, completed);
            }
            else
            {
                _rewardIcon.sprite = service.Configuration.SilverIcon;
                _rewardIcon.material = null;
                _rewardIcon.color = Color.white;
                _rewardIcon.preserveAspect = true;
            }
        }

        public void OnSelect(BaseEventData eventData) => RequestSelection();

        private void RequestSelection()
        {
            if (Quest != null)
                Selected?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(RequestSelection);
        }
    }
}
