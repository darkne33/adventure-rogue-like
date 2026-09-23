using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Features.Quests.Scripts
{
    public sealed class QuestsPanelView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button _closeButton;
        [SerializeField] private UnityEngine.UI.Button _hideCompletedButton;
        [SerializeField] private UnityEngine.UI.Toggle _hideCompletedCheck;
        [SerializeField] private TMP_Text _total;
        [SerializeField] private RectTransform _totalProgressFill;
        [SerializeField] private UnityEngine.UI.ScrollRect _scroll;
        [SerializeField] private TMP_Text _emptyLabel;
        [SerializeField] private GameObject _detailIconFrame;
        [SerializeField] private UnityEngine.UI.Image _detailIcon;
        [SerializeField] private TMP_Text _detailCondition;
        [SerializeField] private TMP_Text _detailReward;
        [SerializeField] private TMP_Text _detailSilver;
        [SerializeField] private UnityEngine.UI.Button _claimButton;
        [SerializeField] private QuestRowView _rowPrefab;

        private readonly List<QuestRowView> _rows = new();
        private QuestService _service;
        private ProgressionMenuUi _ui;
        private Func<string, Sprite> _getPortrait;
        private Material _portraitMaterial;
        private QuestDefinition _selected;
        private bool _hideCompleted;
        private bool _isOpen;
        private bool _rebuilding;
        private bool _resetScroll;

        public event Action BackRequested;

        public static QuestsPanelView Create(QuestsPanelView prefab, Transform parent,
            QuestService service, Func<string, Sprite> getPortrait = null, Material portraitMaterial = null)
        {
            QuestsPanelView view = Instantiate(prefab, parent, false);
            view.Initialize(service, getPortrait, portraitMaterial);
            return view;
        }

        public void Initialize(QuestService service, Func<string, Sprite> getPortrait = null,
            Material portraitMaterial = null)
        {
            _service = service;
            _getPortrait = getPortrait;
            _portraitMaterial = portraitMaterial;
            _ui = new ProgressionMenuUi(service.Configuration, getPortrait, portraitMaterial);
            _closeButton.onClick.AddListener(RequestBack);
            _hideCompletedButton.onClick.AddListener(ToggleHideCompleted);
            _claimButton.onClick.AddListener(ClaimSelectedReward);
            gameObject.SetActive(false);
        }

        public void Show()
        {
            if (_isOpen)
                return;
            _isOpen = true;
            _selected = null;
            _resetScroll = true;
            _service.Changed += Refresh;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Hide()
        {
            if (_service != null)
                _service.Changed -= Refresh;
            _isOpen = false;
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            _total.text = $"Completed: {_service.CompletedCount} / {_service.TotalCount}";
            float fraction = _service.TotalCount > 0 ? (float)_service.CompletedCount / _service.TotalCount : 0f;
            _totalProgressFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            _hideCompletedCheck.SetIsOnWithoutNotify(_hideCompleted);
            RebuildRows();
        }

        private void RebuildRows()
        {
            string previousId = _selected?.Id;
            GameObject focused = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject : null;
            bool focusRow = focused == null || !focused.transform.IsChildOf(transform);
            focusRow |= focused == _claimButton.gameObject;
            Vector2 position = _resetScroll ? Vector2.zero : _scroll.content.anchoredPosition;
            _resetScroll = false;
            _rebuilding = true;
            foreach (QuestRowView row in _rows)
            {
                focusRow |= focused == row.gameObject;
                row.Selected -= SelectQuest;
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
            _rows.Clear();
            QuestRowView selected = null;
            foreach (QuestDefinition quest in _service.Definitions)
            {
                if (_hideCompleted && _service.IsCompleted(quest.Id) && !_service.CanClaimReward(quest.Id))
                    continue;
                QuestRowView row = Instantiate(_rowPrefab, _scroll.content, false);
                row.Bind(quest, _service, _rows.Count, _getPortrait, _portraitMaterial);
                row.Selected += SelectQuest;
                row.gameObject.SetActive(true);
                _rows.Add(row);
                if (quest.Id == previousId)
                    selected = row;
            }
            bool empty = _rows.Count == 0;
            _emptyLabel.gameObject.SetActive(empty);
            RefreshNavigation();
            if (!empty)
            {
                selected ??= _rows[0];
                if (focusRow)
                    ProgressionMenuUi.Focus(selected.Button);
                SelectQuest(selected);
            }
            else
            {
                _selected = null;
                _emptyLabel.text = _hideCompleted && _service.TotalCount > 0
                    ? "All quests completed!" : "No quests available.";
                _detailIconFrame.SetActive(false);
                _detailCondition.text = _emptyLabel.text;
                _detailReward.text = _hideCompleted && _service.TotalCount > 0
                    ? "Turn off Hide completed to view finished quests." : string.Empty;
                _detailSilver.text = string.Empty;
                _claimButton.gameObject.SetActive(false);
                if (focusRow)
                    ProgressionMenuUi.Focus(_hideCompletedButton);
            }
            Canvas.ForceUpdateCanvases();
            position.y = Mathf.Clamp(position.y, 0f,
                Mathf.Max(0f, _scroll.content.rect.height - _scroll.viewport.rect.height));
            _scroll.StopMovement();
            _scroll.content.anchoredPosition = position;
            _rebuilding = false;
            RefreshNavigation();
        }

        private void SelectQuest(QuestRowView row)
        {
            _selected = row.Quest;
            bool complete = _service.IsCompleted(row.Quest.Id);
            UnlockDefinition unlock = _service.GetUnlockForQuest(row.Quest.Id);
            _detailIconFrame.SetActive(true);
            if (unlock != null)
                _ui.Icon(_detailIcon, unlock, complete);
            else
            {
                _detailIcon.sprite = _service.Configuration.SilverIcon;
                _detailIcon.material = null;
                _detailIcon.color = Color.white;
                _detailIcon.preserveAspect = true;
            }
            _detailCondition.text = row.Quest.Description;
            _detailReward.text = unlock != null
                ? $"{RewardCategory(unlock.Category)} - {unlock.DisplayName}" : row.Quest.Title;
            _detailSilver.text = row.Quest.SilverReward > 0
                ? $"+{row.Quest.SilverReward} silver" + (_service.IsRewardClaimed(row.Quest.Id)
                    ? " received" : _service.CanClaimReward(row.Quest.Id) ? " available" : string.Empty)
                : string.Empty;
            _claimButton.gameObject.SetActive(_service.CanClaimReward(row.Quest.Id));
            RefreshNavigation();
            if (!_rebuilding)
                ProgressionMenuUi.EnsureVisible(_scroll, row.Rect);
        }

        private void RefreshNavigation()
        {
            UnityEngine.UI.Selectable first = _rows.Count > 0 ? _rows[0].Button : _closeButton;
            UnityEngine.UI.Selectable footer = _claimButton.gameObject.activeSelf ? _claimButton : _closeButton;
            UnityEngine.UI.Selectable selected = first;
            foreach (QuestRowView row in _rows)
                if (row.Quest == _selected)
                    selected = row.Button;
            for (int i = 0; i < _rows.Count; i++)
                ProgressionMenuUi.Navigate(_rows[i].Button,
                    i > 0 ? _rows[i - 1].Button : _hideCompletedButton,
                    i + 1 < _rows.Count ? _rows[i + 1].Button : footer,
                    _hideCompletedButton, footer);
            ProgressionMenuUi.Navigate(_hideCompletedButton, _closeButton, first, _closeButton, first);
            ProgressionMenuUi.Navigate(_claimButton, selected, _closeButton, selected, _closeButton);
            ProgressionMenuUi.Navigate(_closeButton, footer == _closeButton ? _hideCompletedButton : footer,
                first, _hideCompletedButton, first);
        }

        private void ClaimSelectedReward()
        {
            if (_selected != null)
                _service.TryClaimReward(_selected.Id);
        }

        private void ToggleHideCompleted()
        {
            _hideCompleted = !_hideCompleted;
            _resetScroll = true;
            Refresh();
        }

        private void Update()
        {
            if (!_isOpen)
                return;
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                (gamepad != null && gamepad.buttonEast.wasPressedThisFrame))
            {
                RequestBack();
                return;
            }
            bool navigationPressed = keyboard != null &&
                (keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame ||
                 keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame);
            navigationPressed |= gamepad != null &&
                (gamepad.dpad.ReadValue().sqrMagnitude > 0.2f || gamepad.leftStick.ReadValue().sqrMagnitude > 0.4f);
            if (navigationPressed && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                UnityEngine.UI.Selectable focus = _rows.Count > 0 ? _rows[0].Button : _hideCompletedButton;
                foreach (QuestRowView row in _rows)
                    if (row.Quest == _selected)
                        focus = row.Button;
                ProgressionMenuUi.Focus(focus);
            }
        }

        private static string RewardCategory(ProgressionCategory category) => category switch
        {
            ProgressionCategory.Characters => "Character",
            ProgressionCategory.Weapons => "Weapon",
            ProgressionCategory.Scrolls => "Scroll",
            ProgressionCategory.Relics => "Relic",
            _ => "Reward"
        };

        private void RequestBack()
        {
            if (_isOpen)
                BackRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (_service != null)
                _service.Changed -= Refresh;
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(RequestBack);
            if (_hideCompletedButton != null)
                _hideCompletedButton.onClick.RemoveListener(ToggleHideCompleted);
            if (_claimButton != null)
                _claimButton.onClick.RemoveListener(ClaimSelectedReward);
        }
    }
}
