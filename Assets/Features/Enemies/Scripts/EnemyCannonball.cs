using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyCannonball : EnemyProjectile
    {
        public void Launch(Vector3 startPosition, Vector3 targetPosition, float flightDuration,
            float arcHeight, float impactRadius, int damage, EnemyFacade source, CharacterFacade target,
            CancellationToken cancellationToken)
        {
            CancellationToken projectileCancellationToken = InitializeProjectile(startPosition, damage,
                source, target, cancellationToken);
            Fly(startPosition, targetPosition, Mathf.Max(0.05f, flightDuration), arcHeight,
                Mathf.Max(0f, impactRadius), projectileCancellationToken).Forget();
        }

        private async UniTaskVoid Fly(Vector3 startPosition, Vector3 targetPosition, float flightDuration,
            float arcHeight, float impactRadius, CancellationToken cancellationToken)
        {
            try
            {
                float elapsed = 0f;

                while (elapsed < flightDuration && IsResolved == false)
                {
                    float progress = Mathf.Clamp01(elapsed / flightDuration);
                    if (UpdatePosition(startPosition, targetPosition, arcHeight, progress) == false)
                        break;

                    elapsed += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                if (IsResolved == false)
                {
                    if (UpdatePosition(startPosition, targetPosition, arcHeight, 1f))
                        ResolveHitAtRadius(impactRadius);
                }
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

        private bool UpdatePosition(Vector3 startPosition, Vector3 targetPosition, float arcHeight,
            float progress)
        {
            Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            nextPosition.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;

            Vector3 direction = nextPosition - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized);

            return TryMoveTo(nextPosition);
        }
    }
}
