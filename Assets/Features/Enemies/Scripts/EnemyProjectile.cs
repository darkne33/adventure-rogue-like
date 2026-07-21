using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    [RequireComponent(typeof(Collider))]
    public abstract class EnemyProjectile : MonoBehaviour
    {
        private CharacterFacade _target;
        private EnemyFacade _source;
        private CancellationTokenSource _linkedCancellationTokenSource;
        private int _damage;
        private bool _isLaunched;
        private bool _isResolved;

        protected bool IsResolved => _isResolved;

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

            _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, gameObject.GetCancellationTokenOnDestroy());
            return _linkedCancellationTokenSource.Token;
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

            if (other.GetComponentInParent<Wall>() != null ||
                other.GetComponentInParent<Obstacle>() != null)
            {
                _isResolved = true;
                _isLaunched = false;
                Destroy(gameObject);
            }
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
