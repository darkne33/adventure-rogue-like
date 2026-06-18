using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using UnityEngine;
using Zenject;

namespace Features.Relics.Scripts
{
    public sealed class RelicManager : ITickable, IDisposable
    {
        private const float MoveDistanceEventStep = 1f;
        private const float MaxTrackedMovementDelta = 15f;
        private const float TimeSlowScale = 0.2f;
        private const float TimeSlowDuration = 3f;
        private const float FatalInvulnerabilityDuration = 1.5f;
        private const string EliteBossDamageEffectId = "elite_boss_damage";
        private const string ExplosiveCrateEffectId = "explosive_crate";
        private const string HexDotStatusId = "hex_dot";
        private const string MeteorEffectId = "meteor";
        private const string StillnessHealStatusId = "stillness_heal";

        private readonly CharacterStatModifierLayer _statModifierLayer;
        private readonly RelicEventBus _eventBus;
        private readonly ICharacterProvider _characterProvider;
        private readonly IRelicVisualEffectService _visualEffectService;
        private readonly List<RelicRuntimeState> _activeRelics = new();

        private float _stillnessTimer;
        private float _pendingMoveDistance;
        private float _totalMoveDistance;
        private Vector3 _lastMovePosition;
        private bool _hasLastMovePosition;

        public IReadOnlyList<RelicRuntimeState> ActiveRelics => _activeRelics;

        public event Action Changed;

        public RelicManager(CharacterStatModifierLayer statModifierLayer, RelicEventBus eventBus,
            ICharacterProvider characterProvider, IRelicVisualEffectService visualEffectService)
        {
            _statModifierLayer = statModifierLayer;
            _eventBus = eventBus;
            _characterProvider = characterProvider;
            _visualEffectService = visualEffectService;

            _eventBus.Hit += HandleHit;
            _eventBus.Kill += HandleKill;
            _eventBus.DamageTaken += HandleDamageTaken;
            _eventBus.Heal += HandleHeal;
            _eventBus.RoomStarted += HandleRoomStarted;
            _eventBus.MoveDistance += HandleMoveDistance;
            _eventBus.BossSpawned += HandleBossSpawned;
            _eventBus.ChestOpened += HandleChestOpened;
        }

        public void Tick()
        {
            CharacterFacade character = _characterProvider.CharacterFacade;
            if (character == null)
            {
                _hasLastMovePosition = false;
                return;
            }

            TrackMoveDistance(character);

            float stillnessHeal = 0f;
            float requiredStillnessTime = 0f;
            foreach (RelicRuntimeState state in _activeRelics)
            {
                foreach (RelicEffectDefinition effect in state.Definition.Effects ?? Array.Empty<RelicEffectDefinition>())
                {
                    if (effect.StatusEffectId != StillnessHealStatusId || state.IsBroken)
                        continue;

                    stillnessHeal += effect.Value * state.StackCount;
                    requiredStillnessTime = Mathf.Max(requiredStillnessTime, effect.Duration);
                }
            }

            if (stillnessHeal <= 0f)
                return;

            Vector3 velocity = character.Rigidbody.linearVelocity;
            velocity.y = 0f;
            _stillnessTimer = velocity.sqrMagnitude <= 0.04f
                ? _stillnessTimer + Time.deltaTime
                : 0f;

            if (_stillnessTimer < requiredStillnessTime)
                return;

            float healed = character.HealthSystem.IncreaseCurrentHealth(stillnessHeal * Time.deltaTime);
            if (healed > 0f)
                _eventBus.PublishHeal(new RelicHealEvent(character, healed));
        }

        public bool AddRelic(RelicDefinition relic)
        {
            if (relic == null)
                return false;

            RelicRuntimeState state = _activeRelics.FirstOrDefault(x => x.Definition == relic);
            if (state != null)
            {
                if (relic.IsUnique || state.StackCount >= relic.MaxStacks)
                    return false;

                state.AddStack();
                AddPassiveModifiers(state, 1);
                Changed?.Invoke();
                return true;
            }

            state = new RelicRuntimeState(relic);
            _activeRelics.Add(state);
            AddPassiveModifiers(state, 1);
            ProcessTrigger(state, RelicTriggerType.OnPickup, null);
            Changed?.Invoke();
            return true;
        }

        public bool GiveRelic(string id, RelicPool pool)
        {
            RelicDefinition relic = pool?.GetById(id);
            return relic != null && AddRelic(relic);
        }

        public bool RemoveRelic(string id)
        {
            RelicRuntimeState state = _activeRelics.FirstOrDefault(x => x.Definition.Id == id);
            if (state == null)
                return false;

            _statModifierLayer.RemoveModifiers(GetModifierSourceId(state));
            _activeRelics.Remove(state);
            Changed?.Invoke();
            return true;
        }

        public void ClearRelics()
        {
            foreach (RelicRuntimeState state in _activeRelics)
                _statModifierLayer.RemoveModifiers(GetModifierSourceId(state));

            _activeRelics.Clear();
            Changed?.Invoke();
        }

        public bool TryCancelFatalDamage(CharacterFacade victim, int incomingDamage)
        {
            foreach (RelicRuntimeState state in _activeRelics)
            {
                if (state.IsBroken)
                    continue;

                RelicEffectDefinition effect = state.Definition.Effects?
                    .FirstOrDefault(x => x.TriggerType == RelicTriggerType.OnFatalDamage);

                if (effect == null || RollEffect(state, effect) == false)
                    continue;

                state.Break();
                victim.HealthSystem.SetCurrentHealth(1f);
                victim.SetTemporaryInvulnerability(FatalInvulnerabilityDuration);
                SlowTime(TimeSlowDuration).Forget();
                Changed?.Invoke();
                return true;
            }

            return false;
        }

        public int ModifyOutgoingDamage(int damage, EnemyFacade target)
        {
            if (damage <= 0 || target == null)
                return damage;

            float multiplier = 1f;
            foreach (RelicRuntimeState state in _activeRelics)
            {
                foreach (RelicEffectDefinition effect in state.Definition.Effects ?? Array.Empty<RelicEffectDefinition>())
                {
                    if (effect.TriggerType != RelicTriggerType.PassiveStat ||
                        effect.StatType != RelicStatType.DamageMultiplier ||
                        effect.EffectPrefabId != EliteBossDamageEffectId)
                        continue;

                    if (IsEliteOrBoss(target))
                        multiplier += Mathf.Max(0f, effect.Value) * state.StackCount;
                }
            }

            return Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
        }

        public string PrintActiveRelics()
        {
            if (_activeRelics.Count == 0)
                return "No active relics.";

            return string.Join(", ", _activeRelics.Select(state =>
                $"{state.Definition.Id} x{state.StackCount}{(state.IsBroken ? " (broken)" : string.Empty)}"));
        }

        public void Dispose()
        {
            _eventBus.Hit -= HandleHit;
            _eventBus.Kill -= HandleKill;
            _eventBus.DamageTaken -= HandleDamageTaken;
            _eventBus.Heal -= HandleHeal;
            _eventBus.RoomStarted -= HandleRoomStarted;
            _eventBus.MoveDistance -= HandleMoveDistance;
            _eventBus.BossSpawned -= HandleBossSpawned;
            _eventBus.ChestOpened -= HandleChestOpened;
        }

        private void HandleHit(RelicHitEvent hitEvent)
        {
            foreach (RelicRuntimeState state in _activeRelics.ToArray())
            {
                ProcessTrigger(state, RelicTriggerType.OnHit, hitEvent);

                if (hitEvent.IsCritical)
                    ProcessTrigger(state, RelicTriggerType.OnCrit, hitEvent);
            }
        }

        private void HandleKill(RelicKillEvent killEvent)
        {
            foreach (RelicRuntimeState state in _activeRelics.ToArray())
                ProcessTrigger(state, RelicTriggerType.OnKill, killEvent);
        }

        private void HandleDamageTaken(RelicDamageTakenEvent damageTakenEvent)
        {
            foreach (RelicRuntimeState state in _activeRelics.ToArray())
                ProcessTrigger(state, RelicTriggerType.OnDamageTaken, damageTakenEvent);
        }

        private void HandleHeal(RelicHealEvent healEvent)
        {
            foreach (RelicRuntimeState state in _activeRelics.ToArray())
                ProcessTrigger(state, RelicTriggerType.OnHeal, healEvent);
        }

        private void HandleRoomStarted(RelicRoomEvent roomEvent)
        {
            ResetMoveTracking(roomEvent.CharacterPosition);

            foreach (RelicRuntimeState state in _activeRelics.ToArray())
                ProcessTrigger(state, RelicTriggerType.OnRoomStart, roomEvent);
        }

        private void HandleMoveDistance(RelicMoveDistanceEvent moveDistanceEvent)
        {
            foreach (RelicRuntimeState state in _activeRelics.ToArray())
                ProcessTrigger(state, RelicTriggerType.OnMoveDistance, moveDistanceEvent);
        }

        private void HandleBossSpawned(RelicBossSpawnEvent bossSpawnEvent)
        {
            foreach (RelicRuntimeState state in _activeRelics.ToArray())
                ProcessTrigger(state, RelicTriggerType.OnBossSpawn, bossSpawnEvent);
        }

        private void HandleChestOpened(Vector3 position)
        {
            foreach (RelicRuntimeState state in _activeRelics.ToArray())
                ProcessTrigger(state, RelicTriggerType.OnChestOpen, position);
        }

        private void ProcessTrigger(RelicRuntimeState state, RelicTriggerType triggerType, object context)
        {
            if (state.IsBroken || state.Definition.Effects == null)
                return;

            foreach (RelicEffectDefinition effect in state.Definition.Effects)
            {
                if (effect.TriggerType != triggerType || RollEffect(state, effect) == false)
                    continue;

                if (TryApplyTriggeredScaling(state, effect, context))
                    continue;

                if (triggerType == RelicTriggerType.OnHit && context is RelicHitEvent hitEvent)
                    ApplyOnHitEffect(state, effect, hitEvent);

                if (triggerType == RelicTriggerType.OnKill && context is RelicKillEvent killEvent)
                    ApplyOnKillEffect(state, effect, killEvent);
            }
        }

        private bool RollEffect(RelicRuntimeState state, RelicEffectDefinition effect)
        {
            string cooldownKey = GetCooldownKey(effect);
            if (state.CooldownTimers.TryGetValue(cooldownKey, out float readyTime) && Time.time < readyTime)
                return false;

            if (UnityEngine.Random.value > effect.GetChance(state.StackCount))
                return false;

            if (effect.Cooldown > 0f)
                state.CooldownTimers[cooldownKey] = Time.time + effect.Cooldown;

            return true;
        }

        private void ApplyOnHitEffect(RelicRuntimeState state, RelicEffectDefinition effect,
            RelicHitEvent hitEvent)
        {
            if (hitEvent.Target == null || hitEvent.Target.HealthSystem.IsDead)
                return;

            if (effect.StatusEffectId == HexDotStatusId)
            {
                ApplyHexDamage(effect, hitEvent).Forget();
                return;
            }

            if (effect.EffectPrefabId == MeteorEffectId)
            {
                _visualEffectService.PlayMeteorImpact(hitEvent.HitPosition, effect.Radius).Forget();
                DealAreaDamage(hitEvent.HitPosition, Mathf.Max(1f, effect.Radius),
                    Mathf.Max(1, Mathf.RoundToInt(hitEvent.Damage * effect.Value)),
                    MeteorEffectId);
                return;
            }
        }

        private void ApplyOnKillEffect(RelicRuntimeState state, RelicEffectDefinition effect,
            RelicKillEvent killEvent)
        {
            if (effect.EffectPrefabId != ExplosiveCrateEffectId ||
                killEvent.SourceId == ExplosiveCrateEffectId)
                return;

            DetonateExplosiveCrate(killEvent.Position, Mathf.Max(1f, effect.Radius),
                Mathf.Max(1, Mathf.RoundToInt(12f * effect.Value))).Forget();
        }

        private async UniTaskVoid ApplyHexDamage(RelicEffectDefinition effect, RelicHitEvent hitEvent)
        {
            EnemyFacade target = hitEvent.Target;
            if (target == null)
                return;

            int ticks = Mathf.Max(1, Mathf.CeilToInt(effect.Duration));
            float tickDelay = effect.Duration / ticks;

            for (int i = 0; i < ticks; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(tickDelay),
                    cancellationToken: target.GetCancellationTokenOnDestroy());

                if (target == null || target.HealthSystem.IsDead)
                    return;

                float value = IsEliteOrBoss(target) && effect.BossValue > 0f
                    ? effect.BossValue
                    : effect.Value;
                float maxHealthDamage = target.HealthSystem.MaxHealth * Mathf.Max(0f, value);
                int damage = Mathf.Max(1, Mathf.RoundToInt(maxHealthDamage));
                int appliedDamage = target.HealthSystem.GetDamage(damage);
                target.EffectsSystem.DealDamage(0.04f);
                PublishRelicKillIfDead(target, HexDotStatusId, appliedDamage);
            }
        }

        private bool TryApplyTriggeredScaling(RelicRuntimeState state, RelicEffectDefinition effect, object context)
        {
            float delta = effect.ScalingType switch
            {
                RelicScalingType.PerKill when context is RelicKillEvent => effect.Value * state.StackCount,
                RelicScalingType.PerChestOpened when context is Vector3 => effect.Value * state.StackCount,
                RelicScalingType.PerMoveDistance when context is RelicMoveDistanceEvent moveDistanceEvent =>
                    effect.Value * Mathf.Max(0f, moveDistanceEvent.Distance) * state.StackCount,
                _ => 0f
            };

            if (delta <= 0f)
                return false;

            ApplyTriggeredScaling(state, effect, delta);
            return true;
        }

        private void ApplyTriggeredScaling(RelicRuntimeState state, RelicEffectDefinition effect, float delta)
        {
            string key = GetScalingCounterKey(effect);
            state.CustomCounters.TryGetValue(key, out float current);

            if (effect.Cap > 0f && current >= effect.Cap)
                return;

            if (effect.Cap > 0f)
                delta = Mathf.Min(delta, effect.Cap - current);

            if (delta <= 0f)
                return;

            state.CustomCounters[key] = current + delta;
            AddStatModifier(state, effect, delta);
            Changed?.Invoke();
        }

        private void TrackMoveDistance(CharacterFacade character)
        {
            Vector3 currentPosition = character.transform.position;
            currentPosition.y = 0f;

            if (_hasLastMovePosition == false)
            {
                ResetMoveTracking(currentPosition);
                return;
            }

            float distance = Vector3.Distance(_lastMovePosition, currentPosition);
            _lastMovePosition = currentPosition;

            if (distance <= 0.001f || distance > MaxTrackedMovementDelta)
                return;

            _pendingMoveDistance += distance;
            _totalMoveDistance += distance;

            while (_pendingMoveDistance >= MoveDistanceEventStep)
            {
                _pendingMoveDistance -= MoveDistanceEventStep;
                _eventBus.PublishMoveDistance(new RelicMoveDistanceEvent(character, MoveDistanceEventStep,
                    _totalMoveDistance));
            }
        }

        private void ResetMoveTracking(Vector3 position)
        {
            position.y = 0f;
            _lastMovePosition = position;
            _pendingMoveDistance = 0f;
            _hasLastMovePosition = true;
        }

        private void AddPassiveModifiers(RelicRuntimeState state, int stackDelta)
        {
            if (state.Definition.Effects == null)
                return;

            foreach (RelicEffectDefinition effect in state.Definition.Effects)
            {
                if (effect.TriggerType == RelicTriggerType.PassiveStat)
                {
                    if (effect.EffectPrefabId == EliteBossDamageEffectId)
                        continue;

                    AddStatModifier(state, effect, effect.Value * stackDelta);
                }
            }
        }

        private void AddStatModifier(RelicRuntimeState state, RelicEffectDefinition effect, float value)
        {
            if (TryMapStat(effect.StatType, out StatType stat) == false)
                return;

            _statModifierLayer.AddModifier(GetModifierSourceId(state), stat, value,
                GetStackingType(effect.StatType, effect.ScalingType));
        }

        private void DealAreaDamage(Vector3 center, float radius, int damage, string sourceId)
        {
            EnemyFacade[] enemies = UnityEngine.Object.FindObjectsByType<EnemyFacade>(FindObjectsSortMode.None);
            foreach (EnemyFacade enemy in enemies)
            {
                if (enemy == null || enemy.HealthSystem.IsDead)
                    continue;

                if ((enemy.transform.position - center).sqrMagnitude > radius * radius)
                    continue;

                int appliedDamage = enemy.HealthSystem.GetDamage(damage);
                if (appliedDamage <= 0)
                    continue;

                enemy.EffectsSystem.DealDamage(0.06f);
                PublishRelicKillIfDead(enemy, sourceId, appliedDamage);
            }
        }

        private void PublishRelicKillIfDead(EnemyFacade enemy, string sourceId, int appliedDamage)
        {
            if (enemy == null || appliedDamage <= 0 || enemy.HealthSystem.IsDead == false)
                return;

            CharacterFacade character = _characterProvider.CharacterFacade;
            if (character == null)
                return;

            _eventBus.PublishKill(new RelicKillEvent(character, enemy, enemy.transform.position, sourceId));
        }

        private async UniTaskVoid DetonateExplosiveCrate(Vector3 position, float radius, int damage)
        {
            await _visualEffectService.PlayExplosiveCrate(position, radius,
                detonationPosition => DealAreaDamage(detonationPosition, radius, damage, ExplosiveCrateEffectId));
        }

        private async UniTaskVoid SlowTime(float duration)
        {
            float previousScale = Time.timeScale;
            Time.timeScale = Mathf.Min(Time.timeScale, TimeSlowScale);
            await UniTask.Delay(TimeSpan.FromSeconds(duration), ignoreTimeScale: true);
            await UniTask.WaitWhile(() => Mathf.Approximately(Time.timeScale, 0f));

            if (Mathf.Approximately(Time.timeScale, TimeSlowScale))
                Time.timeScale = previousScale;
        }

        private static string GetCooldownKey(RelicEffectDefinition effect) =>
            $"{effect.TriggerType}_{effect.StatType}_{effect.StatusEffectId}_{effect.EffectPrefabId}";

        private static string GetScalingCounterKey(RelicEffectDefinition effect) =>
            $"{GetCooldownKey(effect)}_{effect.ScalingType}";

        private static string GetModifierSourceId(RelicRuntimeState state) =>
            $"relic:{state.Definition.Id}";

        private static CharacterStatModifierStackingType GetStackingType(RelicStatType statType,
            RelicScalingType scalingType)
        {
            if (scalingType == RelicScalingType.Flat)
                return CharacterStatModifierStackingType.Flat;

            if (scalingType == RelicScalingType.MultiplicativePercent)
                return CharacterStatModifierStackingType.MultiplicativePercent;

            return statType is RelicStatType.MoveSpeed or RelicStatType.MaxHP
                ? CharacterStatModifierStackingType.PercentOfBase
                : CharacterStatModifierStackingType.AdditivePercent;
        }

        private static bool TryMapStat(RelicStatType relicStatType, out StatType stat)
        {
            switch (relicStatType)
            {
                case RelicStatType.DamageMultiplier:
                    stat = StatType.Damage;
                    return true;
                case RelicStatType.AttackSpeedMultiplier:
                    stat = StatType.AttackSpeed;
                    return true;
                case RelicStatType.CritChance:
                    stat = StatType.CritChance;
                    return true;
                case RelicStatType.CritDamage:
                    stat = StatType.CritDamage;
                    return true;
                case RelicStatType.MoveSpeed:
                    stat = StatType.MovementSpeed;
                    return true;
                case RelicStatType.MaxHP:
                    stat = StatType.MaxHp;
                    return true;
                case RelicStatType.HPRegen:
                    stat = StatType.RegenHp;
                    return true;
                case RelicStatType.Armor:
                    stat = StatType.Armor;
                    return true;
                case RelicStatType.Evasion:
                    stat = StatType.Evasion;
                    return true;
                case RelicStatType.Luck:
                    stat = StatType.Luck;
                    return true;
                case RelicStatType.GoldBonus:
                    stat = StatType.GainGold;
                    return true;
                case RelicStatType.XPBonus:
                    stat = StatType.XPBonus;
                    return true;
                case RelicStatType.PickupRange:
                    stat = StatType.PickupRange;
                    return true;
                case RelicStatType.ProjectileCount:
                    stat = StatType.ProjectileCount;
                    return true;
                case RelicStatType.CooldownReduction:
                    stat = StatType.CooldownReduction;
                    return true;
                case RelicStatType.Thorns:
                    stat = StatType.ThornsDamage;
                    return true;
                default:
                    stat = default;
                    return false;
            }
        }

        private static bool IsEliteOrBoss(EnemyFacade target)
        {
            if (target.Configuration == null)
                return false;

            return target.Configuration.EnemyRank is EnemyRank.Elite or EnemyRank.Boss;
        }
    }
}
