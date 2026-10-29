using UnityEngine;

namespace Features.EnemyTargeting
{
    [DisallowMultipleComponent]
    public sealed class EnemyTargetMarker : MonoBehaviour
    {
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private TargetArrowGraphic _graphic;
        [SerializeField] private CanvasGroup _canvasGroup;

        public void Show(Vector2 localPosition, Vector2 size, Vector2 direction, bool focused)
        {
            _rectTransform.anchoredPosition = localPosition;
            _rectTransform.sizeDelta = size;
            _rectTransform.localScale = Vector3.one;
            float angle = focused ? 0f : Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            _graphic.raycastTarget = false;
            _graphic.SetFocusFrame(focused);
            _canvasGroup.alpha = 1f;
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
