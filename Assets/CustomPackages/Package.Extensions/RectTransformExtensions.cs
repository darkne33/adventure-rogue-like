using UnityEngine;

namespace CustomPackages.Package.Extensions
{
    public static class RectTransformExtensions
    {
        public static RectTransform CastToRectTransform(this Transform panelBase)
        {
            return (RectTransform)panelBase.transform;
        }

        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }
        
        public static void SetScreenPositionFromWorld(this RectTransform rectTransform, 
            Vector3 targetWorldPosition, UnityEngine.Camera camera, RectTransform UiRoot)
        {
            var viewportPoint = camera.WorldToViewportPoint(targetWorldPosition);
            var screenPoint = new Vector3
            {
                x = UiRoot.sizeDelta.x * viewportPoint.x,
                y = UiRoot.sizeDelta.y * viewportPoint.y,
                z = 0
            };

            rectTransform.anchoredPosition3D = screenPoint;
        }
    }
}