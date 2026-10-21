using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Features.Sounds;

namespace Features.Enemies.Scripts
{
    [RequireComponent(typeof(Collider))]
    public abstract class EnemyProjectile : MonoBehaviour
    {
        private int _blockingLayerMask;

        private CharacterFacade _target;
        private EnemyFacade _source;
        private CancellationTokenSource _linkedCancellationTokenSource;
        private Collider _collider;
        private float _damage;
        private bool _isLaunched;
        private bool _isResolved;
        private ISoundsService _sounds;
        private SoundId _impactSound;
        private bool _impactSoundPlayed;
        private readonly RaycastHit[] _castHits = new RaycastHit[16];
        private readonly Collider[] _overlaps = new Collider[16];

        protected bool IsResolved => _isResolved;
        protected virtual bool ResolvesDirectHits => true;

        private void Awake() =>
            _blockingLayerMask = LayerMask.GetMask("Obstacle", "Wall");

        protected CancellationToken InitializeProjectile(Vector3 startPosition, float damage,
            EnemyFacade source, CharacterFacade target, CancellationToken cancellationToken)
        {
            CancelCurrentFlight();

            _target = target;
            _source = source;
            _sounds = source != null ? source.Sounds : null;
            _impactSound = source != null && source.Configuration.EnemyDamageType == EnemyDamageType.RangeDirection
                ? SoundId.EnemyNukeImpact
                : source != null && source.SpawnType == EnemyType.Ghost
                    ? SoundId.EnemyGhostImpact : SoundId.EnemyLavaImpact;
            _impactSoundPlayed = false;
            _damage = damage;
            _isLaunched = true;
            _isResolved = false;
            transform.position = startPosition;
            _collider = GetComponent<Collider>();

            _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, gameObject.GetCancellationTokenOnDestroy());
            return _linkedCancellationTokenSource.Token;
        }

        protected bool TryMoveTo(Vector3 nextPosition)
        {
            if (_isResolved)
                return false;

            Vector3 displacement = nextPosition - transform.position;
            float distance = displacement.magnitude;
            Vector3 direction = distance > 0.0001f ? displacement / distance : Vector3.zero;
            Vector3 center = GetCollisionCenter();
            float radius = GetCollisionRadius();
            int mask = _blockingLayerMask;
            if (ResolvesDirectHits && _target != null && _target.Collider != null)
                mask |= 1 << _target.Collider.gameObject.layer;

            // Resolve existing wall/body overlaps before testing shields farther along the flight.
            int overlapCount = Physics.OverlapSphereNonAlloc(center, radius, _overlaps, mask,
                QueryTriggerInteraction.Collide);
            Collider[] overlaps = overlapCount == _overlaps.Length
                ? Physics.OverlapSphere(center, radius, mask, QueryTriggerInteraction.Collide)
                : _overlaps;
            if (overlapCount == _overlaps.Length)
                overlapCount = overlaps.Length;
            bool touchesBody = false;
            for (int index = 0; index < overlapCount; index++)
            {
                Collider other = overlaps[index];
                if (IsBlockingCollider(other))
                {
                    ResolveBlocked();
                    return false;
                }
                touchesBody |= IsTargetBody(other);
            }
            if (touchesBody)
            {
                ResolveHit();
                Destroy(gameObject);
                return false;
            }

            int hitCount = distance > 0.0001f
                ? Physics.SphereCastNonAlloc(center, radius, direction, _castHits, distance, mask,
                    QueryTriggerInteraction.Collide)
                : 0;
            RaycastHit[] hits = hitCount == _castHits.Length
                ? Physics.SphereCastAll(center, radius, direction, distance, mask,
                    QueryTriggerInteraction.Collide)
                : _castHits;
            if (hitCount == _castHits.Length)
                hitCount = hits.Length;

            float nearestDistance = distance;
            Collider nearestCollider = null;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = hits[index];
                if (hit.distance <= nearestDistance &&
                    (IsBlockingCollider(hit.collider) || IsTargetBody(hit.collider)))
                {
                    nearestDistance = hit.distance;
                    nearestCollider = hit.collider;
                }
            }

            // A shield can only intercept the actual swept volume before its first wall/body hit.
            if (_damage > 0f && _target != null &&
                _target.TryBlockProjectile(center, center + direction * nearestDistance, radius,
                    _source, out float blockDistance))
            {
                transform.position += direction * blockDistance;
                ResolveBlocked();
                return false;
            }

            if (nearestCollider != null)
            {
                transform.position += direction * nearestDistance;
                if (IsTargetBody(nearestCollider))
                {
                    ResolveHit();
                    Destroy(gameObject);
                }
                else
                    ResolveBlocked();
                return false;
            }

            transform.position = nextPosition;
            return true;
        }

        protected void ResolveHit() =>
            ResolveHitAtRadius(null);

        protected void ResolveHitAtRadius(float impactRadius) =>
            ResolveHitAtRadius((float?)Mathf.Max(0f, impactRadius));

        private void ResolveHitAtRadius(float? impactRadius)
        {
            if (_isResolved)
                return;

            _isResolved = true;
            _isLaunched = false;

            PlayImpactSound();

            if (_target == null)
                return;

            if (impactRadius.HasValue)
            {
                Vector3 offset = _target.transform.position - transform.position;
                offset.y = 0f;
                float radius = impactRadius.Value;

                if (offset.sqrMagnitude > radius * radius)
                    return;
            }

            _target.ReceiveDamage(_damage, _source, allowAegisBlock: false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isLaunched == false || _isResolved)
                return;

            if (IsTargetBody(other))
            {
                ResolveHit();
                Destroy(gameObject);
                return;
            }

            // Prepared shield triggers are character children, but are not the damageable body.
            if (_damage > 0f && _target != null && other != null &&
                other.GetComponentInParent<AegisShieldView>() != null &&
                other.GetComponentInParent<CharacterFacade>() == _target)
            {
                // Reuse the overlap ordering so a shield beyond an existing wall/body hit is not spent.
                TryMoveTo(transform.position);
                return;
            }

            if (IsBlockingCollider(other))
                ResolveBlocked();
        }

        private float GetCollisionRadius()
        {
            if (_collider == null)
                return 0.01f;

            Vector3 extents = _collider.bounds.extents;
            return Mathf.Max(0.01f, Mathf.Min(extents.x, extents.y, extents.z));
        }

        private Vector3 GetCollisionCenter() => _collider switch
        {
            SphereCollider sphere => sphere.transform.TransformPoint(sphere.center),
            BoxCollider box => box.transform.TransformPoint(box.center),
            CapsuleCollider capsule => capsule.transform.TransformPoint(capsule.center),
            null => transform.position,
            _ => _collider.bounds.center
        };

        private bool IsTargetBody(Collider other) => ResolvesDirectHits && _target != null &&
            other != null && other == _target.Collider;

        private bool IsBlockingCollider(Collider other) =>
            other != null && other != _collider &&
            ((_blockingLayerMask & (1 << other.gameObject.layer)) != 0 ||
            other.GetComponentInParent<Wall>() != null ||
            other.GetComponentInParent<Obstacle>() != null);

        private void ResolveBlocked()
        {
            if (_isResolved)
                return;

            _isResolved = true;
            _isLaunched = false;
            PlayImpactSound();
            Destroy(gameObject);
        }

        private void PlayImpactSound()
        {
            if (_impactSoundPlayed)
                return;
            _impactSoundPlayed = true;
            _sounds?.Play(_impactSound);
        }

        private void CancelCurrentFlight()
        {
            if (_linkedCancellationTokenSource == null)
                return;

            _linkedCancellationTokenSource.Cancel();
            _linkedCancellationTokenSource.Dispose();
            _linkedCancellationTokenSource = null;
        }

        private void OnDestroy() =>
            CancelCurrentFlight();
    }
}
