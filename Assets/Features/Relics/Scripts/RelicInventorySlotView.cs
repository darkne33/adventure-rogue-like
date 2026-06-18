using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Relics.Scripts
{
    public sealed class RelicInventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _borderImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _stackText;

        private RelicRuntimeState _state;
        private RelicTooltipView _tooltipView;

        public void Construct(RelicRuntimeState state, RelicTooltipView tooltipView)
        {
            _state = state;
            _tooltipView = tooltipView;

            if (_borderImage != null)
                _borderImage.color = GetRarityColor(state.Definition.Rarity);

            if (_iconImage != null)
            {
                _iconImage.sprite = state.Definition.Icon;
                _iconImage.preserveAspect = true;
            }

            if (_stackText != null)
            {
                _stackText.text = state.IsBroken ? "X" : state.StackCount.ToString();
                _stackText.color = state.IsBroken ? Color.red : Color.white;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_state == null)
                return;

            _tooltipView?.Show(_state, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData) =>
            _tooltipView?.Hide();

#if UNITY_EDITOR
        public void SetEditorReferences(Image borderImage, Image iconImage, Text stackText)
        {
            _borderImage = borderImage;
            _iconImage = iconImage;
            _stackText = stackText;
        }
#endif

        private static Color GetRarityColor(RelicRarity rarity) =>
            rarity switch
            {
                RelicRarity.Common => new Color(0.28f, 0.85f, 0.28f),
                RelicRarity.Uncommon => new Color(0.2f, 0.55f, 1f),
                RelicRarity.Rare => new Color(0.85f, 0.25f, 1f),
                RelicRarity.Legendary => new Color(1f, 0.75f, 0.12f),
                _ => Color.white
            };
    }
}
