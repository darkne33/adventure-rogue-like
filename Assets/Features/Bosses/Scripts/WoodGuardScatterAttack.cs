using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Features.Bosses.Scripts
{
    public sealed class WoodGuardScatterAttack : IBossAttack
    {
        private const int PositionSamplingAttempts = 24;
        private readonly WoodGuardScatterAttackConfiguration _configuration;
        private readonly BossFacade _boss;
        private readonly CharacterFacade _character;
        private readonly WoodGuardSingleAttack _singleAttack;

        public WoodGuardScatterAttack(WoodGuardScatterAttackConfiguration configuration,
            BossFacade boss, CharacterFacade character)
        {
            _configuration = configuration;
            _boss = boss;
            _character = character;
            _singleAttack = new WoodGuardSingleAttack(configuration, boss, character);
        }

        public async UniTask Execute(CancellationToken cancellationToken, bool animateBoss = true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!CanContinue())
                return;

            WoodGuardSingleAttackPiece prefab = _configuration.PiecePrefab;
            if (prefab == null || !prefab.HasHitCollider ||
                (_configuration.ShowWarning && _configuration.IndicatorPrefab == null))
            {
                Debug.LogError("Scatter wood attack needs a piece prefab with an assigned Sphere Collider " +
                               "and a sphere indicator prefab when warnings are enabled.", _boss);
                return;
            }
            if (_configuration.GetPieceGeometry().Radius <= 0f)
            {
                Debug.LogError("Scatter wood attack piece collider must have a non-zero radius.", prefab);
                return;
            }

            int spawnCount = Mathf.Max(1, _configuration.SpawnCount);
            float maxRadius = NonNegative(_configuration.MaxRadius, 6f);
            float minRadius = Mathf.Min(NonNegative(_configuration.MinRadius, 2f), maxRadius);
            float interval = NonNegative(_configuration.SpawnInterval, 0.6f);
            float warningDuration = _configuration.SafeWarningDuration;
            float impactTime = NonNegative(_configuration.AnimationImpactTime, 1f);
            float recoveryDuration = animateBoss
                ? Mathf.Max(0f, _boss.AnimationSystem.AttackDuration - impactTime) : 0f;
            float minimumDistance = NonNegative(_configuration.MinimumPositionDistance, 1.5f);
            float nextSpawnTime = NonNegative(_configuration.InitialDelay, 1f);
            float angle = Mathf.Repeat(FiniteOr(_configuration.StartingAngle, 0f), 360f);
            float angularStep = Mathf.Repeat(FiniteOr(_configuration.AngularStep, 90f), 360f);
            Vector3 initialCenter = _character.transform.position;
            var positions = new List<Vector3>();
            var pieces = new List<UniTask>();
            using var sequenceCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken sequenceToken = sequenceCancellation.Token;
            float elapsed = 0f;
            bool animationStarted = false;
            bool windupInProgress = false;
            Action<float> onWarningProgress = animateBoss ? progress =>
            {
                animationStarted = true;
                _boss.AnimationSystem.SetAttackWindupProgress(progress, warningDuration, impactTime);
                if (progress >= 1f)
                {
                    windupInProgress = false;
                    nextSpawnTime = Mathf.Max(nextSpawnTime, elapsed + recoveryDuration);
                }
            } : null;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!CanContinue() || HasFailedPiece(pieces))
                        return;

                    float timeScale = _boss.RelicTimeScale;
                    if (animateBoss)
                        _boss.AnimationSystem.SetTimeScale(timeScale);
                    if (timeScale > 0f && _boss.CanAttack)
                    {
                        while (positions.Count < spawnCount && elapsed >= nextSpawnTime && !windupInProgress)
                        {
                            Vector3 center = _configuration.FollowCharacter
                                ? _character.transform.position : initialCenter;
                            center.y = _boss.AttackOrigin.position.y;
                            Vector3 position = ChoosePosition(center, minRadius, maxRadius,
                                minimumDistance, angle, positions);

                            positions.Add(position);
                            windupInProgress = animateBoss;
                            pieces.Add(_singleAttack.ExecuteAt(position, false,
                                _configuration.ShowWarning, sequenceToken, onWarningProgress));
                            angle = Mathf.Repeat(angle + angularStep, 360f);
                            nextSpawnTime = elapsed + interval;
                            if (!CanContinue() || HasFailedPiece(pieces))
                                return;
                        }

                        elapsed += Time.deltaTime * timeScale;
                    }

                    if (positions.Count == spawnCount && AllPiecesCompleted(pieces))
                        break;
                    await UniTask.Yield(PlayerLoopTiming.Update, sequenceToken);
                }
            }
            finally
            {
                sequenceCancellation.Cancel();
                Exception firstFailure = null;
                // Every piece is awaited once, including siblings canceled after an early exit or fault.
                foreach (UniTask piece in pieces)
                {
                    try
                    {
                        await piece;
                    }
                    catch (OperationCanceledException) when (sequenceToken.IsCancellationRequested)
                    {
                    }
                    catch (Exception exception)
                    {
                        firstFailure ??= exception;
                    }
                }

                if (animateBoss && animationStarted && _boss != null && !_boss.IsDead && _boss.isActiveAndEnabled)
                    _boss.AnimationSystem.IdleAnimation();
                cancellationToken.ThrowIfCancellationRequested();
                if (firstFailure != null)
                    ExceptionDispatchInfo.Capture(firstFailure).Throw();
            }
        }

        private Vector3 ChoosePosition(Vector3 center, float minRadius, float maxRadius,
            float minimumDistance, float sequentialAngle, List<Vector3> previousPositions)
        {
            bool sequential = _configuration.PositionMode == WoodGuardScatterPositionMode.SequentialSides;
            if (_configuration.PositionMode == WoodGuardScatterPositionMode.RandomCircle)
                minRadius = 0f;
            float minimumDistanceSquared = minimumDistance * minimumDistance;
            float bestDistanceSquared = -1f;
            Vector3 bestPosition = center;

            for (int attempt = 0; attempt < PositionSamplingAttempts; attempt++)
            {
                float angle = (sequential ? sequentialAngle : Random.Range(0f, 360f)) * Mathf.Deg2Rad;
                float radiusRatio = maxRadius > 0f ? minRadius / maxRadius : 0f;
                float radius = maxRadius * Mathf.Sqrt(Mathf.Lerp(radiusRatio * radiusRatio, 1f, Random.value));
                Vector3 candidate = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
                float nearestDistanceSquared = float.PositiveInfinity;
                foreach (Vector3 previousPosition in previousPositions)
                {
                    Vector3 separation = candidate - previousPosition;
                    separation.y = 0f;
                    nearestDistanceSquared = Mathf.Min(nearestDistanceSquared, separation.sqrMagnitude);
                }
                if (nearestDistanceSquared >= minimumDistanceSquared)
                    return candidate;
                if (nearestDistanceSquared > bestDistanceSquared)
                {
                    bestDistanceSquared = nearestDistanceSquared;
                    bestPosition = candidate;
                }
            }
            // Crowded or zero-radius settings must still finish without moving outside the chosen area.
            return bestPosition;
        }

        private bool CanContinue() => _boss != null && !_boss.IsDead && _boss.isActiveAndEnabled &&
            _character != null && !_character.HealthSystem.IsDead && !_character.IsTransitionPaused;

        private static bool HasFailedPiece(List<UniTask> pieces)
        {
            foreach (UniTask piece in pieces)
            {
                UniTaskStatus status = piece.Status;
                if (status == UniTaskStatus.Faulted || status == UniTaskStatus.Canceled)
                    return true;
            }
            return false;
        }

        private static bool AllPiecesCompleted(List<UniTask> pieces)
        {
            foreach (UniTask piece in pieces)
            {
                if (piece.Status == UniTaskStatus.Pending)
                    return false;
            }
            return true;
        }

        private static float NonNegative(float value, float fallback) => Mathf.Max(0f, FiniteOr(value, fallback));

        private static float FiniteOr(float value, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
    }
}
