using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Relics.Scripts
{
    public sealed class RelicInventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _slotBackgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private UnityEngine.UI.Outline _iconOutline;
        [SerializeField] private Text _stackText;

        private RelicRuntimeState _state;
        private RelicTooltipView _tooltipView;

        public void Construct(RelicRuntimeState state, RelicTooltipView tooltipView)
        {
            _state = state;
            _tooltipView = tooltipView;

            if (_slotBackgroundImage != null)
                _slotBackgroundImage.color = Color.clear;

            if (_iconImage != null)
            {
                _iconImage.sprite = state.Definition.Icon;
                _iconImage.preserveAspect = true;
            }

            if (_iconOutline != null)
            {
                _iconOutline.effectColor = RelicRarityPalette.GetColor(state.Definition.Rarity);
                _iconOutline.effectDistance = GetOutlineDistance(state.Definition.Rarity);
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
        public void SetEditorReferences(Image slotBackgroundImage, Image iconImage,
            UnityEngine.UI.Outline iconOutline, Text stackText)
        {
            _slotBackgroundImage = slotBackgroundImage;
            _iconImage = iconImage;
            _iconOutline = iconOutline;
            _stackText = stackText;
        }
#endif

        private static Vector2 GetOutlineDistance(RelicRarity rarity) =>
            rarity switch
            {
                RelicRarity.Common => new Vector2(1f, -1f),
                RelicRarity.Uncommon => new Vector2(1.35f, -1.35f),
                RelicRarity.Rare => new Vector2(1.7f, -1.7f),
                RelicRarity.Legendary => new Vector2(2.1f, -2.1f),
                _ => new Vector2(1f, -1f)
            };
    }
}
