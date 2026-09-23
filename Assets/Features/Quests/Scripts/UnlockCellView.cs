using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Quests.Scripts
{
    public sealed class UnlockCellView : MonoBehaviour, ISelectHandler
    {
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private UnityEngine.UI.Image _background;
        [SerializeField] private UnityEngine.UI.Image _icon;
        [SerializeField] private RectTransform _priceRoot;
        [SerializeField] private TMP_Text _priceLabel;
        [SerializeField] private TMP_Text _alert;
        [SerializeField] private Color _ownedColor = new(0.43f, 0.43f, 0.43f);
        [SerializeField] private Color _lockedColor = new(0.12f, 0.12f, 0.12f);
        [SerializeField] private Color _purchaseIconColor = new(0.29f, 0.29f, 0.29f);

        private Action _selected;

        public UnlockDefinition Unlock { get; private set; }
        public UnityEngine.UI.Button Button => _button;
        public UnityEngine.UI.Image Icon => _icon;

        public void Bind(UnlockDefinition unlock, Action selected)
        {
            Unlock = unlock;
            _selected = selected;
            _priceLabel.text = unlock.SilverCost.ToString("N0");
            _button.onClick.RemoveListener(Select);
            _button.onClick.AddListener(Select);
        }

        public void RefreshState(bool owned, bool requirementMet)
        {
            _background.color = owned ? _ownedColor : _lockedColor;
            if (!owned && requirementMet)
                _icon.color = _purchaseIconColor;
            _priceRoot.gameObject.SetActive(!owned && requirementMet);
            _alert.gameObject.SetActive(!owned && requirementMet);
        }

        public void OnSelect(BaseEventData eventData) => Select();

        private void Select() => _selected?.Invoke();

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Select);
        }
    }
}
