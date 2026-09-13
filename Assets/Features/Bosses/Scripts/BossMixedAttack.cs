using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    public sealed class BossMixedAttack : IBossAttack
    {
        private readonly BossMixedAttackConfiguration _configuration;
        private readonly BossFacade _boss;
        private readonly CharacterFacade _character;

        public BossMixedAttack(BossMixedAttackConfiguration configuration,
            BossFacade boss, CharacterFacade character)
        {
            _configuration = configuration;
            _boss = boss;
            _character = character;
        }

        public async UniTask Execute(CancellationToken cancellationToken, bool animateBoss = true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!CanContinue())
                return;

            var schedule = new List<ScheduledAttack>();
            if (!TryAddAttacks(_configuration, 0f, schedule, new HashSet<BossMixedAttackConfiguration>()) ||
                schedule.Count == 0)
                return;

            ScheduledAttack animationOwner = schedule[0];
            foreach (ScheduledAttack attack in schedule)
                if (attack.StartDelay < animationOwner.StartDelay)
                    animationOwner = attack;

            using var mixtureCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken mixtureToken = mixtureCancellation.Token;
            float elapsed = 0f;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!CanContinue() || HasFailedAttack(schedule))
                        return;

                    float timeScale = _boss.RelicTimeScale;
                    if (animateBoss)
                        _boss.AnimationSystem.SetTimeScale(timeScale);
                    if (timeScale > 0f && _boss.CanAttack)
                    {
                        foreach (ScheduledAttack attack in schedule)
                        {
                            if (attack.Started || elapsed < attack.StartDelay)
                                continue;

                            // Each entry gets its own runtime instance, even when a config is reused.
                            IBossAttack instance = attack.Configuration.CreateAttack(_boss, _character);
                            attack.Task = instance.Execute(mixtureToken, animateBoss && attack == animationOwner);
                            attack.Started = true;
                            if (!CanContinue() || HasFailedAttack(schedule))
                                return;
                        }
                        elapsed += Time.deltaTime * timeScale;
                    }

                    if (AllAttacksCompleted(schedule))
                        break;
                    await UniTask.Yield(PlayerLoopTiming.Update, mixtureToken);
                }
            }
            finally
            {
                mixtureCancellation.Cancel();
                Exception firstFailure = null;
                foreach (ScheduledAttack attack in schedule)
                {
                    if (!attack.Started)
                        continue;
                    try
                    {
                        await attack.Task;
                    }
                    catch (OperationCanceledException) when (mixtureToken.IsCancellationRequested)
                    {
                    }
                    catch (Exception exception)
                    {
                        firstFailure ??= exception;
                    }
                }

                if (animateBoss && _boss != null && !_boss.IsDead && _boss.isActiveAndEnabled)
                    _boss.AnimationSystem.IdleAnimation();
                cancellationToken.ThrowIfCancellationRequested();
                if (firstFailure != null)
                    ExceptionDispatchInfo.Capture(firstFailure).Throw();
            }
        }

        private bool TryAddAttacks(BossAttackConfiguration configuration, float startDelay,
            List<ScheduledAttack> schedule, HashSet<BossMixedAttackConfiguration> ancestors)
        {
            if (configuration == null || !configuration.IsEnabled)
                return true;
            if (configuration is not BossMixedAttackConfiguration mixture)
            {
                schedule.Add(new ScheduledAttack { Configuration = configuration, StartDelay = startDelay });
                return true;
            }

            if (!ancestors.Add(mixture))
            {
                Debug.LogError("Mixed boss attacks cannot contain circular references. " +
                               $"Remove the loop involving '{mixture.name}'.", _configuration);
                return false;
            }

            try
            {
                if (mixture.Attacks == null)
                    return true;
                foreach (BossMixedAttackEntry entry in mixture.Attacks)
                {
                    if (entry == null || !entry.IsEnabled)
                        continue;
                    float delay = entry.StartDelay;
                    if (float.IsNaN(delay) || float.IsInfinity(delay))
                        delay = 0f;
                    float totalDelay = (float)Math.Min(float.MaxValue, (double)startDelay + Mathf.Max(0f, delay));
                    if (!TryAddAttacks(entry.Attack, totalDelay, schedule, ancestors))
                        return false;
                }
                return true;
            }
            finally
            {
                ancestors.Remove(mixture);
            }
        }

        private bool CanContinue() => _boss != null && !_boss.IsDead && _boss.isActiveAndEnabled &&
            _character != null && !_character.HealthSystem.IsDead && !_character.IsTransitionPaused;

        private static bool HasFailedAttack(List<ScheduledAttack> schedule)
        {
            foreach (ScheduledAttack attack in schedule)
            {
                if (!attack.Started)
                    continue;
                UniTaskStatus status = attack.Task.Status;
                if (status == UniTaskStatus.Faulted || status == UniTaskStatus.Canceled)
                    return true;
            }
            return false;
        }

        private static bool AllAttacksCompleted(List<ScheduledAttack> schedule)
        {
            foreach (ScheduledAttack attack in schedule)
                if (!attack.Started || attack.Task.Status == UniTaskStatus.Pending)
                    return false;
            return true;
        }

        private sealed class ScheduledAttack
        {
            public BossAttackConfiguration Configuration;
            public float StartDelay;
            public bool Started;
            public UniTask Task;
        }
    }
}
