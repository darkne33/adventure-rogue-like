using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public sealed class EnemyCannonball : EnemyProjectile
    {
        [SerializeField] private GameObject _impactEffectPrefab;
        [SerializeField] private EnemyAreaDamageIndicatorView _impactIndicatorPrefab;
        [SerializeField] private Vector3 _impactEffectOffset = new(0f, 0.1f, 0f);
        [SerializeField, Min(0.1f)] private float _impactEffectLifetime = 3f;

        private EnemyAreaDamageIndicatorView _impactIndicator;

        protected override bool ResolvesDirectHits => false;

        public void Launch(Vector3 startPosition, Vector3 targetPosition, float flightDuration,
            float arcHeight, float impactRadius, int damage, EnemyFacade source, CharacterFacade target,
            CancellationToken cancellationToken)
        {
            float safeFlightDuration = Mathf.Max(0.05f, flightDuration);
            float safeImpactRadius = Mathf.Max(0f, impactRadius);
            CancellationToken projectileCancellationToken = InitializeProjectile(startPosition, damage,
                source, target, cancellationToken);
            CreateImpactIndicator(targetPosition, safeImpactRadius, safeFlightDuration);
            Fly(startPosition, targetPosition, safeFlightDuration, arcHeight,
                safeImpactRadius, projectileCancellationToken).Forget();
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
                    {
                        _impactIndicator?.Complete(targetPosition);
                        SpawnImpactEffect(targetPosition);
                        ResolveHitAtRadius(impactRadius);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                DestroyImpactIndicator();

                if (this != null)
                    Destroy(gameObject);
            }
        }

        private void CreateImpactIndicator(Vector3 targetPosition, float impactRadius,
            float flightDuration)
        {
            if (_impactIndicatorPrefab == null)
                return;

            _impactIndicator = Instantiate(
                _impactIndicatorPrefab, targetPosition, Quaternion.identity);
            _impactIndicator.Initialize();
            _impactIndicator.Show(targetPosition, impactRadius, flightDuration);
        }

        private void SpawnImpactEffect(Vector3 impactPosition)
        {
            if (_impactEffectPrefab == null)
                return;

            GameObject impactEffect = Instantiate(
                _impactEffectPrefab, impactPosition + _impactEffectOffset, Quaternion.identity);
            Destroy(impactEffect, Mathf.Max(0.1f, _impactEffectLifetime));
        }

        private void DestroyImpactIndicator()
        {
            if (_impactIndicator == null)
                return;

            _impactIndicator.Hide();
            Destroy(_impactIndicator.gameObject);
            _impactIndicator = null;
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
