using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    [RequireComponent(typeof(Collider))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private CharacterFacade _target;
        private EnemyFacade _source;
        private CancellationTokenSource _linkedCancellationTokenSource;
        private int _damage;
        private float _impactRadius;
        private bool _isLaunched;
        private bool _isResolved;

        public void Launch(Vector3 startPosition, Vector3 targetPosition, float flightDuration, float arcHeight,
            float impactRadius, int damage, EnemyFacade source, CharacterFacade target,
            CancellationToken cancellationToken)
        {
            CancelCurrentFlight();

            _target = target;
            _source = source;
            _damage = damage;
            _impactRadius = impactRadius;
            _isLaunched = true;
            _isResolved = false;
            transform.position = startPosition;

            _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, gameObject.GetCancellationTokenOnDestroy());
            Fly(startPosition, targetPosition, Mathf.Max(0.05f, flightDuration), arcHeight,
                _linkedCancellationTokenSource.Token).Forget();
        }

        private async UniTaskVoid Fly(Vector3 startPosition, Vector3 targetPosition, float flightDuration,
            float arcHeight, CancellationToken cancellationToken)
        {
            try
            {
                float elapsed = 0f;

                while (elapsed < flightDuration)
                {
                    float progress = Mathf.Clamp01(elapsed / flightDuration);
                    UpdatePosition(startPosition, targetPosition, arcHeight, progress);

                    elapsed += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                UpdatePosition(startPosition, targetPosition, arcHeight, 1f);
                ResolveHit(true);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (this != null)
                    Destroy(gameObject);
            }
        }

        private void UpdatePosition(Vector3 startPosition, Vector3 targetPosition, float arcHeight, float progress)
        {
            Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            nextPosition.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;

            Vector3 direction = nextPosition - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized);

            transform.position = nextPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isLaunched == false || _isResolved)
                return;

            CharacterFacade character = other.GetComponentInParent<CharacterFacade>();
            if (character != null && character == _target)
            {
                ResolveHit(false);
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

        private void ResolveHit(bool requireRadiusCheck)
        {
            if (_isResolved)
                return;

            _isResolved = true;
            _isLaunched = false;

            if (_target == null)
                return;

            if (requireRadiusCheck &&
                Vector3.Distance(transform.position, _target.transform.position) > _impactRadius)
                return;

            _target.ReceiveDamage(_damage, _source);
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
