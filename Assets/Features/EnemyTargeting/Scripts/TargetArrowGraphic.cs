using UnityEngine;

namespace Features.EnemyTargeting
{
    // An authored uGUI graphic keeps the yellow pointers crisp at any Canvas scale.
    // All components and hierarchy are supplied by EnemyTargetMarker.prefab.
    [DisallowMultipleComponent]
    public sealed class TargetArrowGraphic : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField] private Color _lightColor = new(1f, 1f, 0.65f, 1f);
        private bool _focusFrame;

        public void SetFocusFrame(bool focused)
        {
            if (_focusFrame == focused)
                return;
            _focusFrame = focused;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vertices)
        {
            vertices.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (!_focusFrame)
            {
                AddPointer(vertices, new Vector2(rect.center.x, rect.yMax), Vector2.down,
                    rect.width, rect.height);
                return;
            }

            float width = Mathf.Min(rect.width, rect.height) * 0.25f;
            float height = width * 0.7f;
            AddPointer(vertices, new Vector2(rect.center.x, rect.yMax), Vector2.down, width, height);
            AddPointer(vertices, new Vector2(rect.xMax, rect.center.y), Vector2.left, width, height);
            AddPointer(vertices, new Vector2(rect.center.x, rect.yMin), Vector2.up, width, height);
            AddPointer(vertices, new Vector2(rect.xMin, rect.center.y), Vector2.right, width, height);
        }

        private void AddPointer(UnityEngine.UI.VertexHelper vertices, Vector2 baseCenter,
            Vector2 direction, float width, float height)
        {
            Vector2 side = new(-direction.y, direction.x);
            Vector2 a = baseCenter + side * (width * 0.5f);
            Vector2 b = baseCenter - side * (width * 0.5f);
            Vector2 tip = baseCenter + direction * height;
            Vector2 center = (a + b + tip) / 3f;
            int start = vertices.currentVertCount;
            vertices.AddVert(a, color, Vector2.zero);
            vertices.AddVert(b, color, Vector2.zero);
            vertices.AddVert(tip, color, Vector2.zero);
            vertices.AddVert(center, new Color(_lightColor.r, _lightColor.g, _lightColor.b,
                _lightColor.a * color.a), Vector2.zero);
            vertices.AddTriangle(start, start + 1, start + 3);
            vertices.AddTriangle(start + 1, start + 2, start + 3);
            vertices.AddTriangle(start + 2, start, start + 3);
        }
    }
}
