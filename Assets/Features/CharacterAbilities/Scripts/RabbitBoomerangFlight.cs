using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

// One piercing throw, followed by a return to the moving owner. Uses the authored prefab hitbox.
public sealed class RabbitBoomerangFlight
{
    private const float MaximumStepTime = 1f / 60f;
    private readonly GameObject _projectile;
    private readonly PlayerCollisionDetector _detector;
    private readonly CharacterFacade _owner;
    private readonly IRelicProjectileContext _context;
    private readonly RabbitBoomerangAbilityConfiguration _configuration;
    private readonly Action<CombatTarget, float> _onHit;
    private readonly Action _onImpact;
    private readonly Animator _animator;
    private readonly RabbitBoomerangTrailView _trail;
    private readonly float _animationSpeed;
    private readonly float _speed;
    private readonly float _radius;
    private readonly int _roomSequence;
    private readonly Dictionary<CombatTarget, float> _nextHits = new();
    private readonly Collider[] _overlaps = new Collider[64];
    private readonly RaycastHit[] _surfaceHits = new RaycastHit[32];
    private Vector3 _outboundDirection;
    private float _age;
    private float _outboundAge;
    private float _returnSpeed;
    private float _distance;
    private int _ricochets;
    private bool _returning;
    private bool _collected;

    public RabbitBoomerangFlight(GameObject projectile, SphereCollider hitbox,
        PlayerCollisionDetector detector, CharacterFacade owner, IRelicProjectileContext context,
        RabbitBoomerangAbilityConfiguration configuration, float speed, Vector3 direction,
        Action<CombatTarget, float> onHit, Action onImpact)
    {
        _projectile = projectile;
        _detector = detector;
        _owner = owner;
        _context = context;
        _configuration = configuration;
        _onHit = onHit;
        _onImpact = onImpact;
        _speed = Mathf.Max(0.1f, speed);
        Vector3 scale = hitbox.transform.lossyScale;
        _radius = Mathf.Max(0.01f, hitbox.radius *
            Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        _outboundDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        _roomSequence = context.RoomSequence;
        _animator = projectile.GetComponent<Animator>();
        _animationSpeed = _animator != null ? _animator.speed : 1f;
        _trail = projectile.GetComponent<RabbitBoomerangTrailView>();
        _trail?.Initialize();
    }

    public async UniTask Run(CancellationToken abilityLifetime)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(abilityLifetime,
            _owner.GetCancellationTokenOnDestroy(), _projectile.GetCancellationTokenOnDestroy(),
            _context.LifetimeToken);
        _detector.ManualCollisionHandling = true;
        try
        {
            float lifetime = Mathf.Max(0.1f, _configuration.MaxLifetime);
            while (!cancellation.IsCancellationRequested && IsAlive() && !_collected && _age < lifetime)
            {
                bool paused = _owner.IsCinematicPaused || Time.deltaTime <= 0f;
                if (_animator != null)
                    _animator.speed = paused ? 0f : _animationSpeed;

                float remainingTime = paused ? 0f : Mathf.Min(Time.deltaTime, lifetime - _age);
                while (remainingTime > 0f && !_collected && !cancellation.IsCancellationRequested && IsAlive())
                {
                    float step = Mathf.Min(MaximumStepTime, remainingTime);
                    Tick(step);
                    _trail?.Tick(step);
                    remainingTime -= step;
                }
                await UniTask.Yield(PlayerLoopTiming.Update, cancellation.Token);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (_projectile != null)
                UnityEngine.Object.Destroy(_projectile);
        }
    }

    private bool IsAlive() => _projectile != null && _projectile.activeInHierarchy &&
        _owner != null && _owner.isActiveAndEnabled && !_owner.HealthSystem.IsDead &&
        !_owner.IsTransitionPaused && _roomSequence == _context.RoomSequence;

    private void Tick(float deltaTime)
    {
        _age += deltaTime;
        if (_returning)
        {
            TickReturn(deltaTime);
            return;
        }

        float duration = Mathf.Max(0.01f, _configuration.OutboundDuration);
        float flightTime = Mathf.Min(deltaTime, Mathf.Max(0f, duration - _outboundAge));
        // Integrate a linear slowdown so speed increases also extend the outgoing range.
        float averageSpeed = _speed * (1f - (_outboundAge + flightTime * 0.5f) / duration);
        _outboundAge += flightTime;
        MoveOutbound(Mathf.Max(0f, averageSpeed * flightTime));
        if (_outboundAge >= duration)
            _returning = true;
    }

    private void TickReturn(float deltaTime)
    {
        Vector3 from = _projectile.transform.position;
        Vector3 toOwner = _owner.ProjectileSpawnPosition - from;
        float collectRadius = Mathf.Max(0.01f, _configuration.CollectRadius);
        if (toOwner.sqrMagnitude <= collectRadius * collectRadius)
        {
            _collected = true;
            return;
        }

        float maximumSpeed = _speed * Mathf.Max(1f, _configuration.ReturnSpeedMultiplier);
        _returnSpeed = Mathf.MoveTowards(_returnSpeed, maximumSpeed,
            Mathf.Max(0.1f, _configuration.ReturnAcceleration) * deltaTime);
        float distance = Mathf.Min(_returnSpeed * deltaTime, toOwner.magnitude);
        Move(from + toOwner.normalized * distance);
        _collected = (_owner.ProjectileSpawnPosition - _projectile.transform.position).sqrMagnitude <=
                     collectRadius * collectRadius;
    }

    private void MoveOutbound(float distance)
    {
        if (distance <= 0f)
            return;

        Vector3 from = _projectile.transform.position;
        if (!FindSurface(from, distance, out RaycastHit surface))
        {
            Move(from + _outboundDirection * distance);
            return;
        }

        float skin = Mathf.Max(0.001f, _configuration.CollisionSkin);
        Move(from + _outboundDirection * Mathf.Max(0f, surface.distance - skin));
        _onImpact();
        if (_ricochets < _context.ProjectileRicochetCount)
        {
            _ricochets++;
            Vector3 normal = surface.normal.sqrMagnitude > 0.0001f
                ? surface.normal.normalized : -_outboundDirection;
            _outboundDirection = Vector3.Reflect(_outboundDirection, normal).normalized;
            _context.PlayRicochet(_projectile.transform.position);
        }
        else
        {
            _returning = true;
        }
    }

    private bool FindSurface(Vector3 from, float distance, out RaycastHit nearest)
    {
        int count = Physics.SphereCastNonAlloc(from, _radius, _outboundDirection, _surfaceHits,
            distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        // Saturated buffers must not hide a closer wall in a dense room.
        RaycastHit[] hits = count == _surfaceHits.Length
            ? Physics.SphereCastAll(from, _radius, _outboundDirection, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            : _surfaceHits;
        if (hits != _surfaceHits)
            count = hits.Length;

        nearest = default;
        float bestDistance = float.MaxValue;
        for (int index = 0; index < count; index++)
        {
            RaycastHit hit = hits[index];
            Collider collider = hit.collider;
            if (hit.distance >= bestDistance || _detector.Ignores(collider) ||
                collider.GetComponentInParent<CombatTarget>() != null ||
                collider.GetComponentInParent<CharacterFacade>() != null ||
                collider.GetComponentInParent<EnemyProjectile>() != null)
                continue;

            nearest = hit;
            bestDistance = hit.distance;
        }
        return bestDistance < float.MaxValue;
    }

    private void Move(Vector3 destination)
    {
        Vector3 from = _projectile.transform.position;
        float distance = Vector3.Distance(from, destination);
        _projectile.transform.position = destination;
        DamageAlongSegment(from, destination, distance);
        _distance += distance;
    }

    private void DamageAlongSegment(Vector3 from, Vector3 to, float distance)
    {
        int count = Physics.OverlapCapsuleNonAlloc(from, to, _radius, _overlaps,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Collider[] overlaps = count == _overlaps.Length
            ? Physics.OverlapCapsule(from, to, _radius, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide)
            : _overlaps;
        if (overlaps != _overlaps)
            count = overlaps.Length;

        Vector3 direction = distance > 0.0001f ? (to - from) / distance : Vector3.zero;
        for (int index = 0; index < count && IsAlive(); index++)
        {
            Collider collider = overlaps[index];
            if (_detector.Ignores(collider))
                continue;

            CombatTarget target = collider.GetComponentInParent<CombatTarget>();
            if (target == null || !target.CanBeAttacked || target.IsDead ||
                !target.gameObject.activeInHierarchy ||
                _nextHits.TryGetValue(target, out float readyAt) && _age < readyAt)
                continue;

            _nextHits[target] = _age + Mathf.Max(0.01f, _configuration.HitCooldown);
            float contactDistance = Mathf.Clamp(Vector3.Dot(collider.bounds.center - from, direction),
                0f, distance);
            _onImpact();
            _onHit(target, _distance + contactDistance);
        }
    }
}
