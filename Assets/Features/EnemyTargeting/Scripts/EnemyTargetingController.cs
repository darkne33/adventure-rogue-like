using System;
using Features.Bosses.Scripts;
using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

namespace Features.EnemyTargeting
{
    public sealed class EnemyTargetingController : ILateTickable, IDisposable
    {
        private readonly IEnemiesProvider _enemies;
        private readonly ICharacterProvider _characters;
        private readonly IRogueLikeRuntimeDataService _runtime;
        private readonly ITimeScaleService _timeScale;
        private readonly RaycastHit[] _occlusionHits = new RaycastHit[32];
        private EnemyTargetingView _view;
        private float _animationTime;
        private bool _disposed;

        public CombatTarget CurrentTarget { get; private set; }

        public EnemyTargetingController(IEnemiesProvider enemies, ICharacterProvider characters,
            IRogueLikeRuntimeDataService runtime, ITimeScaleService timeScale)
        {
            _enemies = enemies;
            _characters = characters;
            _runtime = runtime;
            _timeScale = timeScale;
            _runtime.RoomChanged += OnRoomChanged;
        }

        public void Attach(EnemyTargetingView view)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EnemyTargetingController));
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (_view == view)
                return;

            Clear();
            _view = view;
            _view.Clear();
        }

        public void Detach(EnemyTargetingView view)
        {
            if (_view != view)
                return;

            Clear();
            _view = null;
        }

        public void LateTick()
        {
            if (_disposed || _view == null)
                return;

            CharacterFacade character = _characters.CharacterFacade;
            EnemyTargetingConfiguration configuration = _view.Configuration;
            if (!_view.IsVisible || configuration == null || _runtime.CurrentRoomData == null ||
                _timeScale.IsPaused || character == null || !character.isActiveAndEnabled ||
                character.HealthSystem == null || character.HealthSystem.IsDead ||
                character.IsTransitionPaused || character.IsCinematicPaused)
            {
                Clear();
                return;
            }

            Camera camera = _view.ResolveWorldCamera();
            if (camera == null || !camera.isActiveAndEnabled ||
                !TryGetEdgeRect(camera, configuration, _view.CanvasScaleFactor, out Rect edgeRect))
            {
                Clear();
                return;
            }

            _view.BeginFrame();
            _animationTime += Time.deltaTime;

            float range = configuration.TargetingRange;
            float rangeSquared = range * range;
            float distanceWeight = configuration.DistanceWeight;
            float centerWeight = configuration.ScreenCenterWeight;
            float weightSum = Mathf.Max(0.0001f, distanceWeight + centerWeight);
            float aspect = camera.pixelRect.width / Mathf.Max(1f, camera.pixelRect.height);
            float centerNormalization = Mathf.Sqrt(aspect * aspect + 1f);
            float bestScore = float.PositiveInfinity;
            float currentScore = float.PositiveInfinity;
            CombatTarget bestTarget = null;
            Vector2 bestScreenPoint = Vector2.zero;
            Vector2 currentScreenPoint = Vector2.zero;

            foreach (CombatTarget target in _enemies.ActiveEnemies)
            {
                if (!CanIndicate(target))
                    continue;

                Vector3 aimPoint = GetAimPoint(target);
                Vector3 viewport = camera.WorldToViewportPoint(aimPoint);
                if (!IsInsideViewport(camera, viewport))
                {
                    Vector2 direction = GetScreenDirection(camera, aimPoint, viewport);
                    Vector2 edgePoint = ProjectToEdge(edgeRect, direction);
                    _view.ShowOffscreen(target, edgePoint, direction);
                    continue;
                }

                float squaredDistance = (target.transform.position - character.transform.position).sqrMagnitude;
                if (squaredDistance > rangeSquared ||
                    !HasLineOfSight(camera.transform.position, aimPoint, character, target,
                        configuration.OcclusionMask))
                    continue;

                Vector2 centerOffset = new((viewport.x - 0.5f) * 2f * aspect,
                    (viewport.y - 0.5f) * 2f);
                float centerScore = Mathf.Clamp01(centerOffset.magnitude / centerNormalization);
                float distanceScore = Mathf.Sqrt(squaredDistance) / range;
                float score = (distanceScore * distanceWeight + centerScore * centerWeight) / weightSum;
                Vector2 screenPoint = camera.WorldToScreenPoint(aimPoint);

                if (target == CurrentTarget)
                {
                    currentScore = score;
                    currentScreenPoint = screenPoint;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                    bestScreenPoint = screenPoint;
                }
            }

            // Keep a still-visible selection until another candidate is meaningfully better.
            if (CurrentTarget != null && !float.IsPositiveInfinity(currentScore) &&
                currentScore <= bestScore + configuration.SwitchHysteresis)
            {
                _view.ShowSelected(CurrentTarget, currentScreenPoint, _animationTime);
            }
            else
            {
                CurrentTarget = bestTarget;
                if (CurrentTarget != null)
                    _view.ShowSelected(CurrentTarget, bestScreenPoint, _animationTime);
            }

            _view.EndFrame();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _runtime.RoomChanged -= OnRoomChanged;
            Clear();
            _view = null;
        }

        private void OnRoomChanged(RoomData previous, RoomData current) => Clear();

        private void Clear()
        {
            CurrentTarget = null;
            _animationTime = 0f;
            if (_view != null)
                _view.Clear();
        }

        private static bool CanIndicate(CombatTarget target) =>
            target != null && target.isActiveAndEnabled && target.HealthSystem != null &&
            !target.IsDead && target.CanBeAttacked &&
            target is not MushroomBossFacade { IsPlayingSplitTransition: true };

        private static Vector3 GetAimPoint(CombatTarget target)
        {
            if (target.TargetToShootDamage != null)
                return target.TargetToShootDamage.position;

            Bounds bounds = target.AreaDamageBounds;
            bool hasBounds = bounds.size.sqrMagnitude > 0.0001f;
            Renderer[] renderers = target.MeshRenderers;
            if (renderers != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        continue;
                    if (hasBounds)
                        bounds.Encapsulate(renderer.bounds);
                    else
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                }
            }

            return hasBounds ? bounds.center : target.transform.position;
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 destination, CharacterFacade character,
            CombatTarget target, LayerMask occlusionMask)
        {
            Vector3 offset = destination - origin;
            float distance = offset.magnitude;
            if (distance <= 0.0001f || occlusionMask.value == 0)
                return true;

            int count = Physics.RaycastNonAlloc(origin, offset / distance, _occlusionHits, distance,
                occlusionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider collider = _occlusionHits[i].collider;
                if (collider == null || collider.transform.IsChildOf(target.transform) ||
                    collider.transform.IsChildOf(character.transform) ||
                    collider.GetComponentInParent<CombatTarget>() != null ||
                    collider.GetComponentInParent<PlayerCollisionDetector>() != null ||
                    collider.GetComponentInParent<EnemyProjectile>() != null)
                    continue;

                return false;
            }

            // A saturated buffer cannot prove visibility beyond the returned hits.
            return count < _occlusionHits.Length;
        }

        private static bool IsInsideViewport(Camera camera, Vector3 point) =>
            point.z >= camera.nearClipPlane && point.z <= camera.farClipPlane &&
            point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;

        private static Vector2 GetScreenDirection(Camera camera, Vector3 worldPoint, Vector3 viewport)
        {
            Vector2 direction = new((viewport.x - 0.5f) * camera.pixelRect.width,
                (viewport.y - 0.5f) * camera.pixelRect.height);
            if (viewport.z < 0f)
                direction = -direction;

            if (float.IsNaN(direction.x) || float.IsNaN(direction.y) ||
                float.IsInfinity(direction.x) || float.IsInfinity(direction.y) ||
                direction.sqrMagnitude < 0.0001f)
            {
                Vector3 local = camera.transform.InverseTransformPoint(worldPoint);
                direction = new Vector2(local.x, local.y);
                if (direction.sqrMagnitude < 0.0001f)
                    direction = local.z < 0f ? Vector2.down : Vector2.up;
            }

            return direction.normalized;
        }

        private static bool TryGetEdgeRect(Camera camera, EnemyTargetingConfiguration configuration,
            float canvasScaleFactor, out Rect edgeRect)
        {
            Rect cameraRect = camera.pixelRect;
            Rect safeArea = Screen.safeArea;
            float left = Mathf.Max(cameraRect.xMin, safeArea.xMin);
            float right = Mathf.Min(cameraRect.xMax, safeArea.xMax);
            float bottom = Mathf.Max(cameraRect.yMin, safeArea.yMin);
            float top = Mathf.Min(cameraRect.yMax, safeArea.yMax);
            if (right <= left || top <= bottom)
            {
                edgeRect = default;
                return false;
            }

            float scale = Mathf.Max(0.0001f, canvasScaleFactor);
            // Half the diagonal contains the entire marker at every outward rotation.
            float markerRadius = configuration.OffscreenSize.magnitude * 0.5f;
            float inset = (configuration.ScreenEdgePadding + markerRadius) * scale;
            float horizontalInset = Mathf.Min(inset, (right - left) * 0.5f);
            float verticalInset = Mathf.Min(inset, (top - bottom) * 0.5f);
            edgeRect = Rect.MinMaxRect(left + horizontalInset, bottom + verticalInset,
                right - horizontalInset, top - verticalInset);
            return true;
        }

        private static Vector2 ProjectToEdge(Rect rect, Vector2 direction)
        {
            Vector2 halfSize = rect.size * 0.5f;
            float horizontal = Mathf.Abs(direction.x) > 0.0001f
                ? halfSize.x / Mathf.Abs(direction.x) : float.PositiveInfinity;
            float vertical = Mathf.Abs(direction.y) > 0.0001f
                ? halfSize.y / Mathf.Abs(direction.y) : float.PositiveInfinity;
            return rect.center + direction * Mathf.Min(horizontal, vertical);
        }
    }
}
