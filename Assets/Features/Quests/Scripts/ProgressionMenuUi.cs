using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Quests.Scripts
{
    // Data binding and navigation only. Every visual element lives in a prefab.
    internal sealed class ProgressionMenuUi
    {
        public static readonly Color Gold = new(1f, 0.89f, 0f);
        public static readonly Color White = new(0.98f, 0.98f, 0.95f);
        public static readonly Color Muted = new(0.73f, 0.78f, 0.76f);
        public static readonly Color Green = new(0.12f, 1f, 0f);
        public static readonly Color PanelColor = new(0.145f, 0.169f, 0.153f, 1f);

        private readonly Func<string, Sprite> _getPortrait;
        private readonly Material _portraitMaterial;
        public ProgressionConfiguration Configuration { get; }

        public ProgressionMenuUi(ProgressionConfiguration configuration,
            Func<string, Sprite> getPortrait, Material portraitMaterial)
        {
            Configuration = configuration;
            _getPortrait = getPortrait;
            _portraitMaterial = portraitMaterial;
        }

        public void Icon(UnityEngine.UI.Image image, UnlockDefinition unlock, bool revealed)
        {
            Sprite portrait = unlock != null && !string.IsNullOrEmpty(unlock.CharacterId)
                ? _getPortrait?.Invoke(unlock.CharacterId) : null;
            image.sprite = portrait != null ? portrait : unlock != null && unlock.Icon != null
                ? unlock.Icon : Configuration.PortraitPlaceholder;
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = true;
            image.material = portrait != null && revealed ? _portraitMaterial : null;
            image.color = revealed ? Color.white : Color.black;
        }

        public static void Navigate(UnityEngine.UI.Selectable item, UnityEngine.UI.Selectable up,
            UnityEngine.UI.Selectable down, UnityEngine.UI.Selectable left, UnityEngine.UI.Selectable right)
        {
            item.navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = up,
                selectOnDown = down,
                selectOnLeft = left,
                selectOnRight = right
            };
        }

        public static void Focus(UnityEngine.UI.Selectable selectable)
        {
            if (EventSystem.current != null && selectable != null)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        public static void EnsureVisible(UnityEngine.UI.ScrollRect scroll, RectTransform item)
        {
            if (!scroll.gameObject.activeInHierarchy)
                return;
            Canvas.ForceUpdateCanvases();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, item);
            Rect viewport = scroll.viewport.rect;
            Vector2 position = scroll.content.anchoredPosition;
            if (bounds.max.y > viewport.yMax)
                position.y -= bounds.max.y - viewport.yMax;
            else if (bounds.min.y < viewport.yMin)
                position.y += viewport.yMin - bounds.min.y;
            position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, scroll.content.rect.height - viewport.height));
            scroll.StopMovement();
            scroll.content.anchoredPosition = position;
        }

        public static string CategoryName(ProgressionCategory category) => category.ToString().ToUpperInvariant();
    }
}
