using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using UnityEngine;

namespace Features.Relics.Scripts
{
    // Owns one flight only. Uses the existing projectile prefab and never adds runtime components.
    public sealed class RelicProjectileFlight
    {
        private const float Skin = 0.025f;
        private readonly GameObject _projectile;
        private readonly PlayerCollisionDetector _detector;
        private readonly CharacterFacade _owner;
        private readonly IEnemiesProvider _enemies;
        private readonly IRelicProjectileContext _context;
        private readonly int _roomSequence;
        private readonly float _speed;
        private readonly float _range;
        private readonly float _nativeBounceRadius;
        private readonly Action<CombatTarget, float> _onHit;
        private readonly Action _onComplete;
        private readonly RaycastHit[] _hits = new RaycastHit[64];
        private readonly HashSet<CombatTarget> _legTargets = new();
        private readonly HashSet<CombatTarget> _nativeTargets = new();
        private readonly Dictionary<CombatTarget, float> _nextHits = new();
        private Vector3 _direction;
        private readonly bool _hasNativeBounces;
        private int _nativeTargetsRemaining;
        private int _bouncesUsed;
        private float _distance;
        private Collider _lastSurface;
        private float _surfaceIgnoreUntil;
        private bool _finished;

        public RelicProjectileFlight(GameObject projectile, PlayerCollisionDetector detector,
            CharacterFacade owner, IEnemiesProvider enemies, IRelicProjectileContext context, float speed,
            float range, Vector3 direction, CombatTarget nativeTarget, int nativeTargetCount,
            float nativeBounceRadius, Action<CombatTarget, float> onHit, Action onComplete)
        {
            _projectile = projectile;
            _detector = detector;
            _owner = owner;
            _enemies = enemies;
            _context = context;
            _roomSequence = context.RoomSequence;
            _speed = Mathf.Max(0.1f, speed);
            _range = Mathf.Max(0.1f, range);
            _direction = direction.normalized;
            _hasNativeBounces = nativeTarget != null && nativeTargetCount > 1;
            _nativeTargetsRemaining = Mathf.Max(0, nativeTargetCount - 1);
            _nativeBounceRadius = nativeBounceRadius;
            _onHit = onHit;
            _onComplete = onComplete;
        }

        public async UniTask Run()
        {
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _owner.GetCancellationTokenOnDestroy(), _projectile.GetCancellationTokenOnDestroy(),
                _context.LifetimeToken);
            _detector.ManualCollisionHandling = true;
            try
            {
                // Both a distance and a time budget prevent trapped ricochets from living forever.
                float lifetime = Mathf.Min(20f, _range / _speed + 1f);
                float age = 0f;
                while (!cancellation.IsCancellationRequested && !_finished &&
                       _projectile != null && _owner != null && _roomSequence == _context.RoomSequence &&
                       !_owner.HealthSystem.IsDead && _distance < _range && age < lifetime)
                {
                    float remainingStep = Mathf.Min(_speed * Time.deltaTime, _range - _distance);
                    for (int contact = 0; contact < 12 && remainingStep > 0.0001f && !_finished; contact++)
                    {
                        Vector3 from = _projectile.transform.position;
                        if (!FindHit(from, remainingStep, out RaycastHit hit))
                        {
                            Move(remainingStep);
                            remainingStep = 0f;
                            break;
                        }
                        float travel = Mathf.Clamp(hit.distance, 0f, remainingStep);
                        Move(travel);
                        remainingStep -= travel;
                        ResolveHit(hit);
                        if (!_finished)
                        {
                            float advance = Mathf.Min(Skin, Mathf.Min(remainingStep, _range - _distance));
                            Move(advance);
                            remainingStep -= advance;
                        }
                    }
                    age += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellation.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (_projectile != null)
                {
                    if (cancellation.IsCancellationRequested || _roomSequence != _context.RoomSequence)
                        UnityEngine.Object.Destroy(_projectile);
                    else
                        _onComplete();
                }
            }
        }

        private void Move(float distance)
        {
            _projectile.transform.position += _direction * distance;
            _projectile.transform.rotation = Quaternion.LookRotation(_direction);
            _distance += distance;
        }

        private bool FindHit(Vector3 from, float distance, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.SphereCastNonAlloc(from, 0.1f, _direction, _hits,
                distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                Collider collider = hit.collider;
                if (hit.distance >= nearestDistance || _detector.Ignores(collider) ||
                    collider.GetComponentInParent<EnemyProjectile>() != null ||
                    collider == _lastSurface && Time.time < _surfaceIgnoreUntil)
                    continue;
                CombatTarget target = collider.GetComponentInParent<CombatTarget>();
                if (target != null)
                {
                    if (target.IsDead || _legTargets.Contains(target) ||
                        _nextHits.TryGetValue(target, out float readyAt) && Time.time < readyAt)
                        continue;
                }
                else if (collider.isTrigger)
                    continue;
                nearest = hit;
                nearestDistance = hit.distance;
            }
            return nearestDistance < float.MaxValue;
        }

        private void ResolveHit(RaycastHit hit)
        {
            CombatTarget target = hit.collider.GetComponentInParent<CombatTarget>();
            if (target != null)
            {
                _legTargets.Add(target);
                _nativeTargets.Add(target);
                _nextHits[target] = Time.time + 0.12f;
                _onHit(target, _distance);

                if (_hasNativeBounces && TryNativeBounce())
                    return;
                if (_context.HasPiercingProjectiles)
                    return;
            }
            if (_bouncesUsed >= _context.ProjectileRicochetCount)
            {
                _finished = true;
                return;
            }
            _bouncesUsed++;
            _lastSurface = hit.collider;
            _surfaceIgnoreUntil = Time.time + 0.05f;
            Vector3 normal = hit.normal.sqrMagnitude > 0.001f ? hit.normal.normalized : -_direction;
            _direction = Vector3.Reflect(_direction, normal).normalized;
            _legTargets.Clear();
            _context.PlayRicochet(_projectile.transform.position);
        }

        private bool TryNativeBounce()
        {
            if (_nativeTargetsRemaining <= 0)
                return false;
            CombatTarget closest = null;
            float best = _nativeBounceRadius * _nativeBounceRadius;
            foreach (CombatTarget candidate in _enemies.ActiveEnemies)
            {
                if (candidate == null || candidate.IsDead || _nativeTargets.Contains(candidate))
                    continue;
                float distance = (TargetPosition(candidate) - _projectile.transform.position).sqrMagnitude;
                if (distance >= best)
                    continue;
                closest = candidate;
                best = distance;
            }
            if (closest == null)
                return false;
            _nativeTargetsRemaining--;
            Vector3 direction = TargetPosition(closest) - _projectile.transform.position;
            if (direction.sqrMagnitude > 0.001f)
                _direction = direction.normalized;
            return true;
        }

        private static Vector3 TargetPosition(CombatTarget target) =>
            target.TargetToShootDamage != null ? target.TargetToShootDamage.position : target.transform.position;
    }
}
