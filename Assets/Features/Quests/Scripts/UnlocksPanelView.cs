using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Features.Quests.Scripts
{
    public sealed class UnlocksPanelView : MonoBehaviour
    {
        private int Columns => Mathf.Max(1, _grid.constraintCount);

        [SerializeField] private UnityEngine.UI.ScrollRect _scroll;
        [SerializeField] private UnityEngine.UI.GridLayoutGroup _grid;
        [SerializeField] private UnityEngine.UI.Button _closeButton;
        [SerializeField] private UnityEngine.UI.Button _purchaseButton;
        [SerializeField] private TMP_Text _purchaseLabel;
        [SerializeField] private UnityEngine.UI.Image _detailIcon;
        [SerializeField] private TMP_Text _detailName;
        [SerializeField] private TMP_Text _detailDescription;
        [SerializeField] private TMP_Text _requirement;
        [SerializeField] private TMP_Text _progress;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private RectTransform _progressBarRoot;
        [SerializeField] private UnityEngine.UI.Image _progressBarFill;
        [SerializeField] private UnlockCellView _cellPrefab;
        [SerializeField] private CategoryTab[] _tabs = Array.Empty<CategoryTab>();
        [SerializeField] private Color _activeTabColor = new(0.11f, 0.13f, 0.11f, 0.85f);
        [SerializeField] private Color _inactiveTabColor = new(0.11f, 0.13f, 0.11f, 0.3f);

        private readonly List<UnlockCellView> _cells = new();
        private QuestService _service;
        private Func<string, Sprite> _getPortrait;
        private Material _portraitMaterial;
        private UnlockDefinition _selected;
        private int _categoryIndex;
        private int _builtCategoryIndex = -1;
        private float _gridWidth;
        private bool _isOpen;

        public event Action BackRequested;

        public static UnlocksPanelView Create(UnlocksPanelView prefab, Transform parent,
            QuestService service, Func<string, Sprite> getPortrait = null, Material portraitMaterial = null)
        {
            UnlocksPanelView view = Instantiate(prefab, parent, false);
            view.Initialize(service, getPortrait, portraitMaterial);
            return view;
        }

        private void Initialize(QuestService service, Func<string, Sprite> getPortrait, Material portraitMaterial)
        {
            _service = service;
            _getPortrait = getPortrait;
            _portraitMaterial = portraitMaterial;
            _closeButton.onClick.AddListener(RequestBack);
            _purchaseButton.onClick.AddListener(PurchaseSelected);
            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;
                _tabs[i].Button.onClick.AddListener(() => SelectCategory(index));
            }
            ClearDetails();
            gameObject.SetActive(false);
        }

        public void Show()
        {
            if (_isOpen)
                return;
            _isOpen = true;
            _selected = null;
            _categoryIndex = 0;
            _builtCategoryIndex = -1;
            _service.Changed += Refresh;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
            Canvas.ForceUpdateCanvases();
            ResizeGrid();
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1f;
            FocusSelectedUnlock();
        }

        public void Hide()
        {
            _service.Changed -= Refresh;
            _isOpen = false;
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool hasNewUnlock = false;
                foreach (UnlockDefinition unlock in _service.Unlocks)
                {
                    if (unlock.Category != _tabs[i].Category || !_service.IsNewUnlock(unlock))
                        continue;
                    hasNewUnlock = true;
                    break;
                }
                _tabs[i].Alert.gameObject.SetActive(hasNewUnlock);
                _tabs[i].Background.color = i == _categoryIndex ? _activeTabColor : _inactiveTabColor;
            }
            if (_builtCategoryIndex != _categoryIndex)
                RebuildGrid();
            if (_selected == null && _cells.Count > 0)
                _selected = _cells[0].Unlock;
            foreach (UnlockCellView cell in _cells)
                RefreshCell(cell);
            RefreshDetails();
            RefreshNavigation();
        }

        private void RebuildGrid()
        {
            foreach (UnlockCellView cell in _cells)
            {
                cell.gameObject.SetActive(false);
                Destroy(cell.gameObject);
            }
            _cells.Clear();
            foreach (UnlockDefinition unlock in _service.Unlocks)
            {
                if (unlock.Category != _tabs[_categoryIndex].Category)
                    continue;
                UnlockCellView cell = Instantiate(_cellPrefab, _scroll.content, false);
                cell.Bind(unlock, () => SelectUnlock(cell));
                cell.gameObject.SetActive(true);
                _cells.Add(cell);
            }
            _builtCategoryIndex = _categoryIndex;
        }

        private void RefreshCell(UnlockCellView cell)
        {
            bool owned = _service.IsOwned(cell.Unlock);
            bool requirementMet = _service.IsRequirementMet(cell.Unlock);
            ApplyIcon(cell.Icon, cell.Unlock, owned || requirementMet);
            cell.RefreshState(owned, requirementMet, _service.IsNewUnlock(cell.Unlock));
        }

        private void SelectUnlock(UnlockCellView cell)
        {
            _selected = cell.Unlock;
            _service.MarkUnlockViewed(cell.Unlock);
            RefreshDetails();
            RefreshNavigation();
            ProgressionMenuUi.EnsureVisible(_scroll, (RectTransform)cell.transform);
        }

        private void RefreshDetails()
        {
            if (_selected == null)
            {
                ClearDetails();
                return;
            }

            bool owned = _service.IsOwned(_selected);
            bool requirementMet = _service.IsRequirementMet(_selected);
            QuestDefinition quest = _service.GetQuest(_selected.RequiredQuestId);
            _detailIcon.enabled = true;
            ApplyIcon(_detailIcon, _selected, owned || requirementMet);
            _detailName.text = _selected.DisplayName.ToUpperInvariant();
            _detailDescription.text = _selected.Description;
            _purchaseButton.gameObject.SetActive(!owned && requirementMet);
            _purchaseButton.interactable = _service.CanPurchase(_selected);
            _progress.gameObject.SetActive(!owned && !requirementMet);
            _progressBarRoot.gameObject.SetActive(!owned && !requirementMet);
            if (owned)
            {
                _requirement.text = _selected.UnlockedByDefault ? "AVAILABLE FROM THE START" : "UNLOCKED";
                _requirement.color = ProgressionMenuUi.Green;
                _status.text = "OWNED";
                _status.color = ProgressionMenuUi.Green;
            }
            else if (requirementMet)
            {
                _requirement.text = "QUEST COMPLETE. PURCHASE TO ADD TO YOUR RUNS.";
                _requirement.color = ProgressionMenuUi.Green;
                _purchaseLabel.text = $"BUY {_selected.SilverCost:N0}";
                _status.text = _service.CanPurchase(_selected) ? "READY TO UNLOCK"
                    : $"NEED {Mathf.Max(0, _selected.SilverCost - _service.Silver):N0} MORE SILVER";
                _status.color = ProgressionMenuUi.Gold;
            }
            else
            {
                _requirement.text = quest != null ? quest.Description : "COMPLETE THE REQUIRED QUEST";
                _requirement.color = ProgressionMenuUi.White;
                int progress = quest != null ? Mathf.Clamp(_service.GetProgress(quest), 0, quest.Target) : 0;
                int target = quest != null ? quest.Target : 1;
                _progress.text = $"{progress:N0} / {target:N0}";
                _progressBarFill.rectTransform.anchorMax = new Vector2(
                    target > 0 ? Mathf.Clamp01((float)progress / target) : 0f, 1f);
                _status.text = "LOCKED";
                _status.color = ProgressionMenuUi.Muted;
            }
        }

        private void ClearDetails()
        {
            _detailIcon.sprite = null;
            _detailIcon.enabled = false;
            _detailName.text = string.Empty;
            _detailDescription.text = string.Empty;
            _requirement.text = string.Empty;
            _progress.text = string.Empty;
            _status.text = string.Empty;
            _progress.gameObject.SetActive(false);
            _progressBarRoot.gameObject.SetActive(false);
            _purchaseButton.gameObject.SetActive(false);
        }

        private void ApplyIcon(UnityEngine.UI.Image image, UnlockDefinition unlock, bool revealed)
        {
            Sprite portrait = !string.IsNullOrEmpty(unlock.CharacterId)
                ? _getPortrait?.Invoke(unlock.CharacterId) : null;
            image.sprite = portrait != null ? portrait : unlock.Icon != null
                ? unlock.Icon : _service.Configuration.PortraitPlaceholder;
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = true;
            image.material = portrait != null && revealed ? _portraitMaterial : null;
            image.color = revealed ? Color.white : Color.black;
        }

        private void PurchaseSelected()
        {
            if (_selected == null || !_service.CanPurchase(_selected))
                return;
            _service.TryPurchase(_selected.Id);
            if (!_service.IsOwned(_selected))
                return;
            foreach (UnlockCellView cell in _cells)
            {
                if (cell.Unlock != _selected)
                    continue;
                ProgressionMenuUi.Focus(cell.Button);
                break;
            }
        }

        private void RefreshNavigation()
        {
            UnityEngine.UI.Selectable first = _cells.Count > 0 ? _cells[0].Button : _closeButton;
            UnityEngine.UI.Selectable footer = _purchaseButton.gameObject.activeSelf && _purchaseButton.interactable
                ? _purchaseButton : _closeButton;
            for (int i = 0; i < _tabs.Length; i++)
                ProgressionMenuUi.Navigate(_tabs[i].Button, _closeButton, first,
                    i > 0 ? _tabs[i - 1].Button : _closeButton,
                    i + 1 < _tabs.Length ? _tabs[i + 1].Button : _closeButton);
            for (int i = 0; i < _cells.Count; i++)
                ProgressionMenuUi.Navigate(_cells[i].Button,
                    i >= Columns ? _cells[i - Columns].Button : _tabs[_categoryIndex].Button,
                    i + Columns < _cells.Count ? _cells[i + Columns].Button : footer,
                    i % Columns > 0 ? _cells[i - 1].Button : _tabs[_categoryIndex].Button,
                    i % Columns < Columns - 1 && i + 1 < _cells.Count ? _cells[i + 1].Button : footer);
            UnityEngine.UI.Selectable selectedCell = first;
            foreach (UnlockCellView cell in _cells)
                if (cell.Unlock == _selected)
                    selectedCell = cell.Button;
            ProgressionMenuUi.Navigate(_purchaseButton, selectedCell, _closeButton, selectedCell, _closeButton);
            ProgressionMenuUi.Navigate(_closeButton, footer == _closeButton ? first : footer,
                first, _tabs[_categoryIndex].Button, first);
        }

        private void SelectCategory(int index)
        {
            int categoryIndex = (index + _tabs.Length) % _tabs.Length;
            _categoryIndex = categoryIndex;
            _selected = null;
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1f;
            Refresh();
            FocusSelectedUnlock();
        }

        private void FocusSelectedUnlock()
        {
            foreach (UnlockCellView cell in _cells)
            {
                if (cell.Unlock != _selected)
                    continue;
                ProgressionMenuUi.Focus(cell.Button);
                return;
            }
            ProgressionMenuUi.Focus(_tabs[_categoryIndex].Button);
        }

        private void ResizeGrid()
        {
            float width = _scroll.viewport.rect.width;
            if (width <= 0f || Mathf.Abs(width - _gridWidth) < 0.5f)
                return;
            _gridWidth = width;
            float size = Mathf.Max(1f, (width - _grid.padding.horizontal - _grid.spacing.x * (Columns - 1)) / Columns);
            _grid.cellSize = new Vector2(size, size);
        }

        private void Update()
        {
            if (!_isOpen)
                return;
            ResizeGrid();
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                (gamepad != null && gamepad.buttonEast.wasPressedThisFrame))
            {
                RequestBack();
                return;
            }
            if ((keyboard != null && keyboard.pageUpKey.wasPressedThisFrame) ||
                (gamepad != null && gamepad.leftShoulder.wasPressedThisFrame))
                SelectCategory(_categoryIndex - 1);
            else if ((keyboard != null && keyboard.pageDownKey.wasPressedThisFrame) ||
                     (gamepad != null && gamepad.rightShoulder.wasPressedThisFrame))
                SelectCategory(_categoryIndex + 1);
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
                FocusSelectedUnlock();
        }

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
            if (_purchaseButton != null)
                _purchaseButton.onClick.RemoveListener(PurchaseSelected);
        }

        [Serializable]
        private sealed class CategoryTab
        {
            public ProgressionCategory Category;
            public UnityEngine.UI.Button Button;
            public UnityEngine.UI.Image Alert;
            public UnityEngine.UI.Image Background;
        }
    }
}
