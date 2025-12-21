using UnityEngine;

namespace CustomPackages.Package.Extensions
{
    public static class TransformExtensions
    {
        public static void Clear(this Transform transform)
        {
            if (Application.isPlaying)
            {
                foreach (Transform child in transform)
                {
                    Object.Destroy(child.gameObject);
                }
            }
            else
            {
                while (transform.childCount > 0)
                {
                    Object.DestroyImmediate(transform.GetChild(0).gameObject);
                }
            }
        }

        public static RectTransform SetSizeDeltaX(this RectTransform rectTransform, float x)
            {
                rectTransform.sizeDelta = new Vector2(x, rectTransform.sizeDelta.y);
                return rectTransform;
            }

            public static Transform SetLossyScale(this Transform transform, float? x = null, float? y = null,
                float? z = null)
            {
                var lossyScale = transform.lossyScale.Change3(x, y, z);

                transform.localScale = Vector3.one;
                transform.localScale = new Vector3(lossyScale.x / transform.lossyScale.x,
                    lossyScale.y / transform.lossyScale.y,
                    lossyScale.z / transform.lossyScale.z);

                return transform;
            }
        }
    }