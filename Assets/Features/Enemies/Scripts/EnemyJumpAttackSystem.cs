using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyJumpAttackSystem : IEnemyDamageSystem, IEnemyMovementSystem
    {
        private const float NavigationSampleDistance = 1.5f;
        private const int LandingAttempts = 10;

        private readonly EnemyFacade _enemy;
        private readonly CharacterFacade _character;
        private readonly EnemyConfiguration _configuration;
        private readonly EnemyJumpAttackConfiguration _jump;
        private readonly IEnemiesProvider _enemiesProvider;

        private EnemyPursuitMovementSystem _pursuitMovement;
        private Rigidbody _body;
        private NavMeshAgent _agent;
        private Collider[] _colliders;
        private bool[] _wasTrigger;
        private bool _useGravity;
        private bool _executing;
        private float _cooldown;
        private int _lastClockFrame = -1;

        public bool CanAttack => true;

        private bool CanContinue => _enemy != null && _enemy.isActiveAndEnabled && !_enemy.IsDead &&
            _character != null && _character.HealthSystem != null && !_character.HealthSystem.IsDead &&
            !_character.IsTransitionPaused;

        public EnemyJumpAttackSystem(EnemyFacade enemy, CharacterFacade character,
            EnemyConfiguration configuration, IEnemiesProvider enemiesProvider)
        {
            _enemy = enemy;
            _character = character;
            _configuration = configuration;
            _jump = configuration.JumpConfiguration;
            _enemiesProvider = enemiesProvider;
        }

        public void Initialize()
        {
            if (_jump == null)
                throw new InvalidOperationException($"{_enemy.name} requires a jump attack configuration.");

            _body = _enemy.Rigidbody;
            _agent = _enemy.GetComponent<NavMeshAgent>();
            _colliders = _enemy.GetComponentsInChildren<Collider>();
            _wasTrigger = new bool[_colliders.Length];
            _pursuitMovement = new EnemyPursuitMovementSystem(_enemy, _character,
                _configuration, _agent, _enemiesProvider);
            _cooldown = Mathf.Max(0.1f, _jump.Interval);
        }

        public void Tick()
        {
            if (_executing)
                StopVelocity();
            else
                _pursuitMovement?.Tick();
        }

        public void Reset()
        {
            if (!_executing)
                _pursuitMovement?.Reset();
        }

        public void OnAttackFinished()
        {
        }

        public async UniTask Tick(CancellationToken cancellationToken)
        {
            while (_enemy != null && !_enemy.IsDead)
            {
                cancellationToken.ThrowIfCancellationRequested();
                float delta = AdvanceClock();
                if (delta > 0f && CanContinue && _enemy.IsAggro && !_enemy.IsStopped &&
                    _enemy.CanAttack && _cooldown <= 0f)
                    await Execute(cancellationToken);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_executing || !CanContinue || !_enemy.IsAggro || _enemy.IsStopped || !_enemy.CanAttack)
                return;
            if (!TryPlanLanding(out Vector3 destination, out Vector3 ground, out float floorOffset))
            {
                _cooldown = 0.25f;
                return;
            }

            Vector3 start = _body.position;
            Vector3 direction = destination - start;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : _enemy.transform.forward;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            float duration = Mathf.Max(0.01f, _jump.Duration);
            float impact = Mathf.Clamp(_jump.ImpactNormalized, 0.01f, 1f);
            float takeoff = Mathf.Clamp(_jump.TakeoffNormalized, 0f, impact - 0.001f);
            GameObject indicator = null;
            Vector3 indicatorScale = Vector3.one;
            bool airborne = false;
            bool hasHit = false;
            float elapsed = 0f;

            _executing = true;
            _cooldown = Mathf.Max(0.1f, _jump.Interval);
            _useGravity = _body.useGravity;
            for (int i = 0; i < _colliders.Length; i++)
                _wasTrigger[i] = _colliders[i] != null && _colliders[i].isTrigger;

            try
            {
                _enemy.SetStop(true);
                StopVelocity();
                _body.useGravity = false;
                _body.rotation = rotation;
                _enemy.transform.rotation = rotation;
                _enemy.AnimationSystem.IdleAnimation();

                // These prepared prefabs arrive as serialized dependencies of the Addressable enemy.
                if (_jump.IndicatorPrefab != null)
                {
                    indicator = Object.Instantiate(_jump.IndicatorPrefab,
                        ground + Vector3.up * _jump.IndicatorGroundOffset, Quaternion.identity);
                    foreach (Collider collider in indicator.GetComponentsInChildren<Collider>(true))
                        collider.enabled = false;
                    indicatorScale = indicator.transform.localScale;
                    UpdateWarning(indicator.transform, indicatorScale, 0f);
                }

                while (elapsed < duration)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!CanContinue)
                        return;

                    float delta = AdvanceClock();
                    if (delta <= 0f)
                        continue;
                    elapsed = Mathf.Min(duration, elapsed + delta);
                    float progress = elapsed / duration;
                    if (!airborne && progress >= takeoff)
                    {
                        airborne = true;
                        SetFlightColliders();
                        _enemy.AnimationSystem.AttackAnimation();
                    }

                    float flight = Mathf.InverseLerp(takeoff, impact, progress);
                    Vector3 position = Vector3.Lerp(start, destination, flight);
                    position.y += 4f * Mathf.Max(0.1f, _jump.Height) * flight * (1f - flight);
                    SetPosition(position);

                    if (!hasHit && indicator != null)
                        UpdateWarning(indicator.transform, indicatorScale, progress / impact);
                    if (!hasHit && progress >= impact)
                    {
                        hasHit = true;
                        DestroyIndicator(indicator);
                        indicator = null;
                        _enemy.AnimationSystem.IdleAnimation();
                        SpawnEffect(ground);
                        ApplyHit(ground, direction);
                    }
                }
            }
            finally
            {
                DestroyIndicator(indicator);
                if (_body != null)
                    _body.useGravity = _useGravity;
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i] != null && _colliders[i].attachedRigidbody == _body)
                        _colliders[i].isTrigger = _wasTrigger[i];
                }

                if (_enemy != null)
                {
                    StopVelocity();
                    if (!_enemy.IsDead)
                    {
                        // Cancellation or a room transition can interrupt the enemy in midair.
                        if (TryGetGround(_body.position, out Vector3 floor))
                            SetPosition(floor + Vector3.up * floorOffset);
                        _enemy.SyncNavigationPosition();
                        _enemy.AnimationSystem.IdleAnimation();
                    }
                    _executing = false;
                    _enemy.SetStop(false);
                }
                else
                {
                    _executing = false;
                }
            }
        }

        private float AdvanceClock()
        {
            if (_lastClockFrame == Time.frameCount)
                return 0f;
            _lastClockFrame = Time.frameCount;
            float delta = CanContinue && _enemy.IsAggro && (_executing || !_enemy.IsStopped)
                ? Time.deltaTime * _enemy.RelicTimeScale : 0f;
            // Count flight and recovery as part of the interval between attack starts.
            _cooldown = Mathf.Max(0f, _cooldown - delta);
            return delta;
        }

        private bool TryPlanLanding(out Vector3 destination, out Vector3 ground, out float floorOffset)
        {
            destination = _body.position;
            ground = destination;
            floorOffset = 0f;
            if (!_agent.isOnNavMesh || !TryGetGround(_body.position, out Vector3 startFloor) ||
                !NavMesh.SamplePosition(startFloor, out NavMeshHit origin,
                    NavigationSampleDistance, _agent.areaMask) ||
                !TryGetGround(_character.transform.position, out Vector3 targetFloor))
                return false;

            floorOffset = Mathf.Max(0f, _body.position.y - startFloor.y);
            bool directTarget = Random.value < Mathf.Clamp01(_jump.DirectTargetChance);
            float bodyRadius = Mathf.Max(0.1f, _agent.radius);
            for (int attempt = 0; attempt < LandingAttempts; attempt++)
            {
                Vector2 offset = (directTarget && attempt == 0) || attempt == LandingAttempts - 1
                    ? Vector2.zero : Random.insideUnitCircle * Mathf.Max(0f, _jump.TargetOffsetRadius);
                Vector3 candidate = targetFloor + new Vector3(offset.x, 0f, offset.y);
                if (!TryGetGround(candidate, out Vector3 floor) ||
                    !NavMesh.SamplePosition(floor, out NavMeshHit landing,
                        NavigationSampleDistance, _agent.areaMask) ||
                    NavMesh.Raycast(origin.position, landing.position, out _, _agent.areaMask) ||
                    !TryGetGround(landing.position, out floor))
                    continue;

                Vector3 route = floor - startFloor;
                route.y = 0f;
                if (route.sqrMagnitude > 0.001f && Physics.SphereCast(
                        startFloor + Vector3.up * (bodyRadius + 0.05f), bodyRadius,
                        route.normalized, out _, route.magnitude, _jump.ObstacleMask,
                        QueryTriggerInteraction.Ignore))
                    continue;

                ground = floor;
                destination = floor + Vector3.up * floorOffset;
                return true;
            }
            return false;
        }

        private bool TryGetGround(Vector3 position, out Vector3 ground)
        {
            float height = Mathf.Max(0.01f, _jump.GroundProbeHeight);
            if (Physics.Raycast(position + Vector3.up * height, Vector3.down, out RaycastHit hit,
                    height + Mathf.Max(0.01f, _jump.GroundProbeDistance), _jump.GroundMask,
                    QueryTriggerInteraction.Ignore) && hit.normal.y >= 0.5f)
            {
                ground = hit.point;
                return true;
            }
            ground = position;
            return false;
        }

        private void UpdateWarning(Transform indicator, Vector3 originalScale, float progress)
        {
            float diameter = Mathf.Max(0.01f, _jump.Radius) * 2f;
            float growth = Mathf.Lerp(Mathf.Clamp(_jump.InitialIndicatorScale, 0.01f, 1f),
                1f, Mathf.Clamp01(progress));
            indicator.localScale = new Vector3(originalScale.x * diameter * growth,
                originalScale.y, originalScale.z * diameter * growth);
        }

        private void SpawnEffect(Vector3 ground)
        {
            if (_jump.EffectPrefab == null)
                return;
            GameObject effect = Object.Instantiate(_jump.EffectPrefab,
                ground + Vector3.up * _jump.IndicatorGroundOffset, Quaternion.identity);
            effect.transform.localScale *= Mathf.Max(0.01f, _jump.EffectScale);
            Object.Destroy(effect, Mathf.Max(0.1f, _jump.EffectLifetime));
        }

        private void ApplyHit(Vector3 ground, Vector3 fallbackDirection)
        {
            // Like the mushroom's landing attack, the shockwave can be avoided by jumping.
            if (!CanContinue || _character.MoveSystem?.IsGrounded == false)
                return;
            float radius = Mathf.Max(0.01f, _jump.Radius);
            Collider[] hits = Physics.OverlapCapsule(ground, ground + Vector3.up * radius, radius,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            foreach (Collider hit in hits)
            {
                if (hit.GetComponentInParent<CharacterFacade>() != _character)
                    continue;
                if (_character.ReceiveDamage(_configuration.Damage, _enemy) && _character.Rigidbody != null &&
                    !_character.Rigidbody.isKinematic)
                {
                    Vector3 direction = _character.transform.position - ground;
                    direction.y = 0f;
                    direction = direction.sqrMagnitude > 0.001f ? direction.normalized : fallbackDirection;
                    _character.Rigidbody.AddForce(direction * Mathf.Max(0f, _jump.KnockbackForce) +
                        Vector3.up * Mathf.Max(0f, _jump.KnockbackUpwardForce), ForceMode.Impulse);
                }
                return;
            }
        }

        private void SetFlightColliders()
        {
            foreach (Collider collider in _colliders)
            {
                if (collider != null && collider.attachedRigidbody == _body)
                    collider.isTrigger = true;
            }
        }

        private void SetPosition(Vector3 position)
        {
            _body.position = position;
            _enemy.transform.position = position;
            StopVelocity();
        }

        private void StopVelocity()
        {
            if (_body == null || _body.isKinematic)
                return;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

        private static void DestroyIndicator(GameObject indicator)
        {
            if (indicator == null)
                return;
            indicator.SetActive(false);
            Object.Destroy(indicator);
        }
    }
}
