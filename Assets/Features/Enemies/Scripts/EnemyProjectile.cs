using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
        private int _damage;
        private bool _isLaunched;
        private bool _isResolved;

        protected bool IsResolved => _isResolved;

        private void Awake() =>
            _blockingLayerMask = LayerMask.GetMask("Obstacle", "Wall");

        protected CancellationToken InitializeProjectile(Vector3 startPosition, int damage,
            EnemyFacade source, CharacterFacade target, CancellationToken cancellationToken)
        {
            CancelCurrentFlight();

            _target = target;
            _source = source;
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

            if (distance > 0.0001f && _blockingLayerMask != 0)
            {
                float radius = GetCollisionRadius();
                if (Physics.SphereCast(transform.position, radius, displacement / distance,
                        out RaycastHit hit, distance, _blockingLayerMask, QueryTriggerInteraction.Collide))
                {
                    transform.position = hit.point;
                    ResolveBlocked();
                    return false;
                }
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

            if (_target == null)
                return;

            if (impactRadius.HasValue &&
                Vector3.Distance(transform.position, _target.transform.position) > impactRadius.Value)
                return;

            _target.ReceiveDamage(_damage, _source);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isLaunched == false || _isResolved)
                return;

            CharacterFacade character = other.GetComponentInParent<CharacterFacade>();
            if (character != null && character == _target)
            {
                ResolveHit();
                Destroy(gameObject);
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

        private bool IsBlockingCollider(Collider other) =>
            (_blockingLayerMask & (1 << other.gameObject.layer)) != 0 ||
            other.GetComponentInParent<Wall>() != null ||
            other.GetComponentInParent<Obstacle>() != null;

        private void ResolveBlocked()
        {
            if (_isResolved)
                return;

            _isResolved = true;
            _isLaunched = false;
            Destroy(gameObject);
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
