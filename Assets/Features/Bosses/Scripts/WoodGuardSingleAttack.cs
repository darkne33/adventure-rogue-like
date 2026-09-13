using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    public sealed class WoodGuardSingleAttack : IBossAttack
    {
        private readonly WoodGuardSingleAttackConfiguration _configuration;
        private readonly BossFacade _boss;
        private readonly CharacterFacade _character;

        public WoodGuardSingleAttack(WoodGuardSingleAttackConfiguration configuration,
            BossFacade boss, CharacterFacade character)
        {
            _configuration = configuration;
            _boss = boss;
            _character = character;
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!CanContinue())
                return;

            WoodGuardSingleAttackPiece prefab = _configuration.PiecePrefab;
            if (prefab == null || !prefab.HasHitCollider || _configuration.IndicatorPrefab == null)
            {
                Debug.LogError("Single wood attack needs a piece prefab with an assigned Sphere Collider " +
                               "and a sphere indicator prefab.", _boss);
                return;
            }

            WoodGuardSingleAttackPiece.Geometry geometry = _configuration.GetPieceGeometry();
            if (geometry.Radius <= 0f)
            {
                Debug.LogError("Single wood attack piece collider must have a non-zero radius.", prefab);
                return;
            }

            // Capture once: moving away during the warning lets the character dodge the hit.
            Vector3 targetPosition = _character.transform.position;
            Quaternion rotation = _boss.AttackRotation;
            Vector3 center = _configuration.GetSphereCenter(targetPosition, geometry.Radius);
            Vector3 surfacePosition = center - rotation * geometry.CenterOffset;
            Vector3 indicatorPosition = surfacePosition + rotation * geometry.IndicatorOffset;
            float undergroundDepth = Mathf.Max(Mathf.Max(0.01f, _configuration.UndergroundDepth),
                geometry.Radius * 2f + 0.01f);
            float warningDuration = _configuration.SafeWarningDuration;
            float attackEnd = warningDuration + _configuration.RootLifetime;
            float elapsed = 0f;
            bool hasEmerged = false;
            GameObject indicator = null;
            WoodGuardSingleAttackPiece roots = null;

            try
            {
                indicator = Object.Instantiate(_configuration.IndicatorPrefab, indicatorPosition, Quaternion.identity);
                foreach (Collider indicatorCollider in indicator.GetComponentsInChildren<Collider>(true))
                    indicatorCollider.enabled = false;
                UpdateWarning(indicator.transform, geometry.Radius, 0f);
                indicator.SetActive(true);
                _boss.AnimationSystem.BeginAttack(warningDuration);

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!CanContinue())
                        return;

                    float timeScale = _boss.RelicTimeScale;
                    _boss.AnimationSystem.SetTimeScale(timeScale);
                    if (timeScale > 0f && _boss.CanAttack)
                    {
                        if (elapsed < warningDuration)
                        {
                            UpdateWarning(indicator.transform, geometry.Radius,
                                elapsed / warningDuration);
                        }
                        else
                        {
                            if (!hasEmerged)
                            {
                                hasEmerged = true;
                                DestroyObject(indicator);
                                roots = Object.Instantiate(prefab,
                                    surfacePosition + Vector3.down * undergroundDepth,
                                    rotation * geometry.RootRotation);
                                roots.EnableObstacle();
                                ApplyHit(center, geometry.Radius, rotation * Vector3.forward);
                                if (!CanContinue() || cancellationToken.IsCancellationRequested)
                                    return;
                                _boss.AnimationSystem.IdleAnimation();
                            }

                            if (roots != null)
                                UpdateRoots(roots.transform, surfacePosition, undergroundDepth,
                                    elapsed - warningDuration);
                            if (elapsed >= attackEnd)
                                break;
                        }
                        elapsed += Time.deltaTime * timeScale;
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            finally
            {
                DestroyObject(indicator);
                if (roots != null)
                    DestroyObject(roots.gameObject);
            }
        }

        private bool CanContinue() => _boss != null && !_boss.IsDead && _boss.isActiveAndEnabled &&
            _character != null && !_character.HealthSystem.IsDead && !_character.IsTransitionPaused;

        private void UpdateWarning(Transform indicator, float radius, float progress)
        {
            float scale = Mathf.Lerp(Mathf.Clamp(_configuration.InitialWarningScale, 0.01f, 1f),
                1f, Mathf.Clamp01(progress));
            indicator.localScale = Vector3.one * (radius * 2f * scale);
        }

        private void UpdateRoots(Transform roots, Vector3 surfacePosition, float undergroundDepth, float time)
        {
            float riseDuration = Mathf.Max(0.01f, _configuration.RiseDuration);
            float holdEnd = riseDuration + Mathf.Max(0f, _configuration.HoldDuration);
            float visibleProgress = time < riseDuration ? Mathf.Clamp01(time / riseDuration) :
                time <= holdEnd ? 1f :
                1f - Mathf.Clamp01((time - holdEnd) / Mathf.Max(0.01f, _configuration.SinkDuration));
            roots.position = surfacePosition + Vector3.down * (undergroundDepth * (1f - visibleProgress));
        }

        private void ApplyHit(Vector3 center, float radius, Vector3 fallbackDirection)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            foreach (Collider hit in hits)
            {
                if (hit.GetComponentInParent<CharacterFacade>() != _character)
                    continue;
                if (_character.ReceiveDamage(_configuration.Damage, _boss) &&
                    _character.Rigidbody != null && !_character.Rigidbody.isKinematic)
                {
                    Vector3 direction = _character.transform.position - center;
                    direction.y = 0f;
                    direction = direction.sqrMagnitude > 0.001f ? direction.normalized : fallbackDirection;
                    _character.Rigidbody.AddForce(direction * Mathf.Max(0f, _configuration.KnockbackForce) +
                        Vector3.up * Mathf.Max(0f, _configuration.KnockbackUpwardForce), ForceMode.Impulse);
                }
                // A character with multiple colliders still receives only one hit.
                return;
            }
        }

        private static void DestroyObject(GameObject instance)
        {
            if (instance == null)
                return;
            instance.SetActive(false);
            Object.Destroy(instance);
        }
    }
}
