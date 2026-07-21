using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyBullet : EnemyProjectile
    {
        public void Launch(Vector3 startPosition, Vector3 direction, float speed, float lifetime,
            int damage, EnemyFacade source, CharacterFacade target, CancellationToken cancellationToken)
        {
            Vector3 normalizedDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : transform.forward;
            transform.rotation = Quaternion.LookRotation(normalizedDirection);

            CancellationToken projectileCancellationToken = InitializeProjectile(startPosition, damage,
                source, target, cancellationToken);
            Fly(normalizedDirection, Mathf.Max(0.1f, speed), Mathf.Max(0.1f, lifetime),
                projectileCancellationToken).Forget();
        }

        private async UniTaskVoid Fly(Vector3 direction, float speed, float lifetime,
            CancellationToken cancellationToken)
        {
            try
            {
                float elapsed = 0f;

                while (elapsed < lifetime && IsResolved == false)
                {
                    transform.position += direction * (speed * Time.deltaTime);
                    elapsed += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
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
    }
}
