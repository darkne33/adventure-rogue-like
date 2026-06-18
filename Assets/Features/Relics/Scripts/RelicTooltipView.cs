using UnityEngine;
using UnityEngine.UI;

namespace Features.Relics.Scripts
{
    public sealed class RelicTooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private Image _background;
        [SerializeField] private Text _text;
        [SerializeField] private Vector2 _screenOffset = new(16f, -16f);

        private void Awake() =>
            Hide();

        public void Show(RelicRuntimeState state, Vector2 screenPosition)
        {
            if (state == null || _root == null || _text == null)
                return;

            _root.gameObject.SetActive(true);
            _root.position = screenPosition + _screenOffset;
            _text.text =
                $"{state.Definition.DisplayName}\n" +
                $"{state.Definition.Rarity} | x{state.StackCount}\n" +
                $"{state.Definition.Description}\n" +
                $"{string.Join(", ", state.Definition.Tags)}" +
                (state.IsBroken ? "\nBROKEN" : string.Empty);
        }

        public void Hide()
        {
            if (_root != null)
                _root.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        public void SetEditorReferences(RectTransform root, Image background, Text text)
        {
            _root = root;
            _background = background;
            _text = text;
        }
#endif
    }
}
