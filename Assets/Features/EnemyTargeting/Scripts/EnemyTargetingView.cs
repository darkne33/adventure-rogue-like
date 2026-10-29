using System.Collections.Generic;
using Core;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Features.EnemyTargeting
{
    [DisallowMultipleComponent]
    public sealed class EnemyTargetingView : MonoBehaviour
    {
        [SerializeField] private EnemyTargetingConfiguration _configuration;
        [SerializeField] private EnemyTargetMarker _markerPrefab;
        [SerializeField] private RectTransform _markerRoot;
        [SerializeField] private CanvasGroup _group;

        [Inject] private DiContainer _container;
        [Inject] private ICameraService _cameraService;

        private readonly Dictionary<CombatTarget, EnemyTargetMarker> _markers = new();
        private readonly Stack<EnemyTargetMarker> _pool = new();
        private readonly HashSet<CombatTarget> _used = new();
        private readonly List<CombatTarget> _unused = new();
        private Canvas _canvas;

        public EnemyTargetingConfiguration Configuration => _configuration;
        public bool IsVisible => isActiveAndEnabled;
        public float CanvasScaleFactor => RootCanvas != null ? RootCanvas.scaleFactor : 1f;

        private Canvas RootCanvas
        {
            get
            {
                if (_canvas == null)
                    _canvas = GetComponentInParent<Canvas>();
                return _canvas != null ? _canvas.rootCanvas : null;
            }
        }

        public Camera ResolveWorldCamera()
        {
            if (_cameraService?.MainCamera != null)
            {
                CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(_cameraService.MainCamera);
                if (brain != null && brain.OutputCamera != null)
                    return brain.OutputCamera;
            }
            return Camera.main;
        }

        public void BeginFrame()
        {
            _used.Clear();
            _group.alpha = 1f;
        }

        public void ShowSelected(CombatTarget target, Vector2 screenPoint, float animationTime)
        {
            float scale = CanvasScaleFactor;
            Vector2 size = _configuration.SelectedSize;
            float bob = Mathf.Sin(animationTime * _configuration.BobFrequency * Mathf.PI * 2f) *
                        _configuration.BobAmplitude;
            screenPoint.y += (_configuration.SelectedVerticalGap + bob) * scale;
            // Keep the focus frame inside the usable camera rectangle near screen edges.
            Camera worldCamera = ResolveWorldCamera();
            if (worldCamera != null)
            {
                Rect viewport = worldCamera.pixelRect;
                Rect safe = Screen.safeArea;
                float halfWidth = size.x * scale * 0.5f;
                float halfHeight = size.y * scale * 0.5f;
                float minX = Mathf.Max(viewport.xMin, safe.xMin) + halfWidth;
                float maxX = Mathf.Min(viewport.xMax, safe.xMax) - halfWidth;
                float minY = Mathf.Max(viewport.yMin, safe.yMin) + halfHeight;
                float maxY = Mathf.Min(viewport.yMax, safe.yMax) - halfHeight;
                if (maxX >= minX && maxY >= minY)
                    screenPoint = new Vector2(Mathf.Clamp(screenPoint.x, minX, maxX),
                        Mathf.Clamp(screenPoint.y, minY, maxY));
            }
            Show(target, screenPoint, size, Vector2.down, true);
        }

        public void ShowOffscreen(CombatTarget target, Vector2 screenPoint, Vector2 screenDirection) =>
            Show(target, screenPoint, _configuration.OffscreenSize, screenDirection, false);

        public void EndFrame()
        {
            _unused.Clear();
            foreach (KeyValuePair<CombatTarget, EnemyTargetMarker> pair in _markers)
                if (!_used.Contains(pair.Key))
                    _unused.Add(pair.Key);

            foreach (CombatTarget target in _unused)
            {
                EnemyTargetMarker marker = _markers[target];
                if (marker != null)
                {
                    marker.Hide();
                    _pool.Push(marker);
                }
                _markers.Remove(target);
            }
            _unused.Clear();
        }

        public void Clear()
        {
            _used.Clear();
            EndFrame();
            _group.alpha = 0f;
        }

        private void Show(CombatTarget target, Vector2 screenPoint, Vector2 size, Vector2 direction, bool focused)
        {
            Canvas canvas = RootCanvas;
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera != null ? canvas.worldCamera : ResolveWorldCamera()
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _markerRoot, screenPoint, uiCamera, out Vector2 localPoint))
                return;

            if (!_markers.TryGetValue(target, out EnemyTargetMarker marker))
            {
                marker = _pool.Count > 0 ? _pool.Pop() :
                    _container.InstantiatePrefabForComponent<EnemyTargetMarker>(_markerPrefab, _markerRoot);
                _markers.Add(target, marker);
            }
            _used.Add(target);
            marker.Show(localPoint, size, direction, focused);
        }

        private void OnDisable() => Clear();

        private void OnDestroy()
        {
            _markers.Clear();
            _pool.Clear();
            _used.Clear();
            _unused.Clear();
        }
    }
}
