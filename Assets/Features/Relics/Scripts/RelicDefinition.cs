using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace Features.Relics.Scripts
{
    public enum RelicRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Legendary = 3
    }

    public enum RelicTag
    {
        Offense,
        Defense,
        Mobility,
        Economy,
        Critical,
        Poison,
        Fire,
        Lightning,
        Ice,
        Explosion,
        Healing,
        Summon,
        Projectile,
        OnHit,
        OnKill,
        OnDamageTaken,
        OnHeal,
        OnFatalDamage,
        Scaling,
        RiskReward,
        BossKiller,
        Utility
    }

    public enum RelicTriggerType
    {
        PassiveStat,
        OnPickup,
        OnRunStart,
        OnRoomStart,
        OnHit,
        OnCrit,
        OnKill,
        OnDamageTaken,
        OnHeal,
        OnMoveDistance,
        OnChestOpen,
        OnBossSpawn,
        OnFatalDamage
    }

    public enum RelicStatType
    {
        DamageMultiplier,
        AttackSpeedMultiplier,
        CritChance,
        CritDamage,
        MoveSpeed,
        MaxHP,
        HPRegen,
        Armor,
        Evasion,
        Luck,
        XPBonus,
        GoldBonus,
        PickupRange,
        ProjectileCount,
        CooldownReduction,
        Thorns
    }

    public enum RelicScalingType
    {
        Flat,
        AdditivePercent,
        MultiplicativePercent,
        PerKill,
        PerMaxHP,
        PerMoveDistance,
        PerMissingHP,
        PerChestOpened,
        OneUse
    }

    [Serializable]
    public sealed class RelicEffectDefinition
    {
        [field: SerializeField] public RelicTriggerType TriggerType { get; private set; }
        [field: SerializeField] public RelicStatType StatType { get; private set; }
        [field: SerializeField] public float Value { get; private set; }
        [field: SerializeField] public float BossValue { get; private set; }
        [field: SerializeField] public float Chance { get; private set; } = 1f;
        [field: SerializeField] public float Cooldown { get; private set; }
        [field: SerializeField] public float Duration { get; private set; }
        [field: SerializeField] public float Radius { get; private set; }
        [field: SerializeField] public float Cap { get; private set; }
        [field: SerializeField] public RelicScalingType ScalingType { get; private set; }
        [field: SerializeField] public string StatusEffectId { get; private set; }
        [field: SerializeField] public string EffectPrefabId { get; private set; }

        public float GetChance(int stacks) =>
            Mathf.Clamp01(Mathf.Max(0f, Chance) * Mathf.Max(1, stacks));
    }

    [CreateAssetMenu(menuName = "Configs/Relics/Relic Definition")]
    public sealed class RelicDefinition : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField, TextArea] public string Description { get; private set; }
        [field: SerializeField] public RelicRarity Rarity { get; private set; }
        [field: SerializeField] public RelicTag[] Tags { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public bool IsUnique { get; private set; }
        [field: SerializeField, Min(1)] public int MaxStacks { get; private set; } = 1;
        [field: SerializeField] public string UnlockQuestId { get; private set; }
        [field: SerializeField] public int UnlockCost { get; private set; }
        [field: SerializeField] public RelicEffectDefinition[] Effects { get; private set; }

        public bool IsLockedByQuest =>
            string.IsNullOrWhiteSpace(UnlockQuestId) == false;
    }

    public sealed class RelicRuntimeState
    {
        public RelicDefinition Definition { get; }
        public int StackCount { get; private set; }
        public bool IsBroken { get; private set; }
        public Dictionary<string, float> CooldownTimers { get; } = new();
        public Dictionary<string, float> CustomCounters { get; } = new();

        public RelicRuntimeState(RelicDefinition definition)
        {
            Definition = definition;
            StackCount = 1;
        }

        public void AddStack() =>
            StackCount++;

        public void Break() =>
            IsBroken = true;
    }

    public sealed class RelicEventBus
    {
        public event Action<RelicHitEvent> Hit;
        public event Action<RelicKillEvent> Kill;
        public event Action<RelicDamageTakenEvent> DamageTaken;
        public event Action<RelicHealEvent> Heal;
        public event Action<Vector3> ChestOpened;
        public event Action<RoomData, Room, Vector3> ChestSpawned;
        public event Action<RoomData, Room> ChestCollected;
        public event Action ChestsCleared;

        public void PublishHit(RelicHitEvent hitEvent) =>
            Hit?.Invoke(hitEvent);

        public void PublishKill(RelicKillEvent killEvent) =>
            Kill?.Invoke(killEvent);

        public void PublishDamageTaken(RelicDamageTakenEvent damageTakenEvent) =>
            DamageTaken?.Invoke(damageTakenEvent);

        public void PublishHeal(RelicHealEvent healEvent) =>
            Heal?.Invoke(healEvent);

        public void PublishChestOpened(Vector3 position) =>
            ChestOpened?.Invoke(position);

        public void PublishChestSpawned(RoomData roomData, Room room, Vector3 position) =>
            ChestSpawned?.Invoke(roomData, room, position);

        public void PublishChestCollected(RoomData roomData, Room room) =>
            ChestCollected?.Invoke(roomData, room);

        public void PublishChestsCleared() =>
            ChestsCleared?.Invoke();
    }

    public readonly struct RelicHitEvent
    {
        public CharacterFacade Attacker { get; }
        public EnemyFacade Target { get; }
        public int Damage { get; }
        public bool IsCritical { get; }
        public string WeaponId { get; }
        public Vector3 HitPosition { get; }

        public RelicHitEvent(CharacterFacade attacker, EnemyFacade target, int damage,
            bool isCritical, string weaponId, Vector3 hitPosition)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
            IsCritical = isCritical;
            WeaponId = weaponId;
            HitPosition = hitPosition;
        }
    }

    public readonly struct RelicKillEvent
    {
        public CharacterFacade Killer { get; }
        public EnemyFacade Target { get; }
        public Vector3 Position { get; }

        public RelicKillEvent(CharacterFacade killer, EnemyFacade target, Vector3 position)
        {
            Killer = killer;
            Target = target;
            Position = position;
        }
    }

    public readonly struct RelicDamageTakenEvent
    {
        public CharacterFacade Victim { get; }
        public EnemyFacade Attacker { get; }
        public int Amount { get; }
        public string DamageType { get; }

        public RelicDamageTakenEvent(CharacterFacade victim, EnemyFacade attacker, int amount,
            string damageType)
        {
            Victim = victim;
            Attacker = attacker;
            Amount = amount;
            DamageType = damageType;
        }
    }

    public readonly struct RelicHealEvent
    {
        public CharacterFacade Target { get; }
        public float Amount { get; }

        public RelicHealEvent(CharacterFacade target, float amount)
        {
            Target = target;
            Amount = amount;
        }
    }

    public sealed class RelicUnlockService
    {
        private readonly HashSet<string> _completedQuests = new();
        private readonly HashSet<string> _unlockedRelics = new();

        public bool IsUnlocked(RelicDefinition relic)
        {
            if (relic == null)
                return false;

            if (relic.IsLockedByQuest == false)
                return true;

            return _unlockedRelics.Contains(relic.Id) ||
                   _completedQuests.Contains(relic.UnlockQuestId);
        }

        public void CompleteQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId) == false)
                _completedQuests.Add(questId);
        }

        public void UnlockRelic(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId) == false)
                _unlockedRelics.Add(relicId);
        }
    }

    public sealed class RelicPool
    {
        private readonly RelicPoolConfiguration _configuration;
        private readonly RelicUnlockService _unlockService;

        public IReadOnlyList<RelicDefinition> AllRelics => _configuration.Relics;

        public RelicPool(RelicPoolConfiguration configuration, RelicUnlockService unlockService)
        {
            _configuration = configuration;
            _unlockService = unlockService;
        }

        public RelicDefinition GetById(string id) =>
            _configuration.Relics.FirstOrDefault(relic => relic != null && relic.Id == id);

        public RelicDefinition Roll(IReadOnlyCollection<RelicRuntimeState> activeRelics,
            IReadOnlyCollection<string> excludedIds = null)
        {
            List<RelicDefinition> candidates = GetAvailable(activeRelics, excludedIds).ToList();
            if (candidates.Count == 0)
                return null;

            int totalWeight = candidates.Sum(relic => Mathf.Max(0, _configuration.GetWeight(relic.Rarity)));
            if (totalWeight <= 0)
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];

            int roll = UnityEngine.Random.Range(0, totalWeight);
            foreach (RelicDefinition relic in candidates)
            {
                roll -= Mathf.Max(0, _configuration.GetWeight(relic.Rarity));
                if (roll < 0)
                    return relic;
            }

            return candidates[^1];
        }

        public IEnumerable<RelicDefinition> GetAvailable(IReadOnlyCollection<RelicRuntimeState> activeRelics,
            IReadOnlyCollection<string> excludedIds = null)
        {
            HashSet<string> excluded = excludedIds != null
                ? new HashSet<string>(excludedIds)
                : new HashSet<string>();

            foreach (RelicDefinition relic in _configuration.Relics)
            {
                if (relic == null || excluded.Contains(relic.Id) || _unlockService.IsUnlocked(relic) == false)
                    continue;

                RelicRuntimeState owned = activeRelics?.FirstOrDefault(state => state.Definition == relic);
                if (owned == null)
                {
                    yield return relic;
                    continue;
                }

                if (relic.IsUnique || owned.StackCount >= relic.MaxStacks)
                    continue;

                yield return relic;
            }
        }
    }

    public sealed class RelicManager : ITickable, IDisposable
    {
        private const float PercentMultiplier = 100f;
        private const float EvasionCap = 60f;
        private const float CritChanceCap = 100f;
        private const float TimeSlowScale = 0.2f;
        private const float TimeSlowDuration = 3f;
        private const float FatalInvulnerabilityDuration = 1.5f;

        private readonly CharacterStats _characterStats;
        private readonly RelicEventBus _eventBus;
        private readonly ICharacterProvider _characterProvider;
        private readonly List<RelicRuntimeState> _activeRelics = new();

        private float _baseMoveSpeed = -1f;
        private float _stillnessTimer;

        public IReadOnlyList<RelicRuntimeState> ActiveRelics => _activeRelics;

        public event Action Changed;

        public RelicManager(CharacterStats characterStats, RelicEventBus eventBus,
            ICharacterProvider characterProvider)
        {
            _characterStats = characterStats;
            _eventBus = eventBus;
            _characterProvider = characterProvider;

            _eventBus.Hit += HandleHit;
            _eventBus.Kill += HandleKill;
            _eventBus.DamageTaken += HandleDamageTaken;
            _eventBus.Heal += HandleHeal;
            _eventBus.ChestOpened += HandleChestOpened;
        }

        public void Tick()
        {
            CharacterFacade character = _characterProvider.CharacterFacade;
            if (character == null)
                return;

            float stillnessHeal = 0f;
            float requiredStillnessTime = 0f;
            foreach (RelicRuntimeState state in _activeRelics)
            {
                foreach (RelicEffectDefinition effect in state.Definition.Effects ?? Array.Empty<RelicEffectDefinition>())
                {
                    if (effect.StatusEffectId != "stillness_heal" || state.IsBroken)
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

            CacheBaseMoveSpeed();

            RelicRuntimeState state = _activeRelics.FirstOrDefault(x => x.Definition == relic);
            if (state != null)
            {
                if (relic.IsUnique || state.StackCount >= relic.MaxStacks)
                    return false;

                state.AddStack();
                ApplyPassiveEffects(relic, 1);
                Changed?.Invoke();
                return true;
            }

            state = new RelicRuntimeState(relic);
            _activeRelics.Add(state);
            ApplyPassiveEffects(relic, 1);
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

            ApplyPassiveEffects(state.Definition, -state.StackCount);
            _activeRelics.Remove(state);
            Changed?.Invoke();
            return true;
        }

        public void ClearRelics()
        {
            foreach (RelicRuntimeState state in _activeRelics)
                ApplyPassiveEffects(state.Definition, -state.StackCount);

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
                        effect.EffectPrefabId != "elite_boss_damage")
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

                if (effect.ScalingType == RelicScalingType.PerKill && context is RelicKillEvent)
                    ApplyPerKillScaling(state, effect);

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

            if (effect.StatusEffectId == "hex_dot")
            {
                ApplyHexDamage(effect, hitEvent).Forget();
                return;
            }

            if (effect.EffectPrefabId == "meteor")
            {
                PlayMeteorEffect(hitEvent.HitPosition, effect.Radius).Forget();
                DealAreaDamage(hitEvent.HitPosition, Mathf.Max(1f, effect.Radius),
                    Mathf.Max(1, Mathf.RoundToInt(hitEvent.Damage * effect.Value)));
                return;
            }
        }

        private void ApplyOnKillEffect(RelicRuntimeState state, RelicEffectDefinition effect,
            RelicKillEvent killEvent)
        {
            if (effect.EffectPrefabId != "explosive_crate")
                return;

            SpawnExplosiveCrate(killEvent.Position, Mathf.Max(1f, effect.Radius),
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
                target.HealthSystem.GetDamage(damage);
                target.EffectsSystem.DealDamage(0.04f);
            }
        }

        private void ApplyPerKillScaling(RelicRuntimeState state, RelicEffectDefinition effect)
        {
            string key = $"{effect.TriggerType}_{effect.StatType}_{effect.ScalingType}";
            state.CustomCounters.TryGetValue(key, out float current);

            if (effect.Cap > 0f && current >= effect.Cap)
                return;

            float delta = effect.Value * state.StackCount;
            if (effect.Cap > 0f)
                delta = Mathf.Min(delta, effect.Cap - current);

            if (delta <= 0f)
                return;

            state.CustomCounters[key] = current + delta;
            ApplyStat(effect.StatType, delta, effect.ScalingType, 1);
            Changed?.Invoke();
        }

        private void ApplyPassiveEffects(RelicDefinition relic, int stackDelta)
        {
            if (relic.Effects == null)
                return;

            foreach (RelicEffectDefinition effect in relic.Effects)
            {
                if (effect.TriggerType == RelicTriggerType.PassiveStat)
                {
                    if (effect.EffectPrefabId == "elite_boss_damage")
                        continue;

                    ApplyStat(effect.StatType, effect.Value, effect.ScalingType, stackDelta);
                }
            }
        }

        private void ApplyStat(RelicStatType statType, float value, RelicScalingType scalingType, int stackDelta)
        {
            float normalizedValue = scalingType == RelicScalingType.Flat ? value : value * PercentMultiplier;
            float delta = normalizedValue * stackDelta;

            switch (statType)
            {
                case RelicStatType.DamageMultiplier:
                    _characterStats.DamageInPercent += delta;
                    break;
                case RelicStatType.AttackSpeedMultiplier:
                    _characterStats.AttackSpeed += delta;
                    break;
                case RelicStatType.CritChance:
                    _characterStats.CritChance = Mathf.Clamp(_characterStats.CritChance + delta, 0f, CritChanceCap);
                    break;
                case RelicStatType.CritDamage:
                    _characterStats.CritDamage += delta;
                    break;
                case RelicStatType.MoveSpeed:
                    CacheBaseMoveSpeed();
                    _characterStats.MovementSpeed += _baseMoveSpeed * value * stackDelta;
                    break;
                case RelicStatType.MaxHP:
                    _characterStats.MaxHp += scalingType == RelicScalingType.Flat
                        ? value * stackDelta
                        : _characterStats.MaxHp * value * stackDelta;
                    break;
                case RelicStatType.HPRegen:
                    _characterStats.RegenHp += scalingType == RelicScalingType.Flat
                        ? value * stackDelta
                        : value * PercentMultiplier * stackDelta;
                    break;
                case RelicStatType.Armor:
                    _characterStats.Armor += delta;
                    break;
                case RelicStatType.Evasion:
                    _characterStats.Evasion = Mathf.Clamp(_characterStats.Evasion + delta, 0f, EvasionCap);
                    break;
                case RelicStatType.Luck:
                    _characterStats.Luck += delta;
                    break;
                case RelicStatType.GoldBonus:
                    _characterStats.GainGold += delta;
                    break;
                case RelicStatType.Thorns:
                    _characterStats.ThornsDamage += scalingType == RelicScalingType.Flat
                        ? value * stackDelta
                        : value * PercentMultiplier * stackDelta;
                    break;
            }
        }

        private void DealAreaDamage(Vector3 center, float radius, int damage)
        {
            EnemyFacade[] enemies = UnityEngine.Object.FindObjectsByType<EnemyFacade>(FindObjectsSortMode.None);
            foreach (EnemyFacade enemy in enemies)
            {
                if (enemy == null || enemy.HealthSystem.IsDead)
                    continue;

                if ((enemy.transform.position - center).sqrMagnitude > radius * radius)
                    continue;

                enemy.HealthSystem.GetDamage(damage);
                enemy.EffectsSystem.DealDamage(0.06f);
            }
        }

        private async UniTaskVoid PlayMeteorEffect(Vector3 position, float radius)
        {
            GameObject meteor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meteor.name = "Relic_Meteor_Impact";
            meteor.transform.position = position + new Vector3(0f, 5f, 0f);
            meteor.transform.localScale = Vector3.one * 0.55f;

            Renderer renderer = meteor.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(1f, 0.33f, 0.06f);

            await meteor.transform.DOMove(position + Vector3.up * 0.4f, 0.16f)
                .SetEase(Ease.InQuad)
                .ToUniTask(cancellationToken: meteor.GetCancellationTokenOnDestroy());

            _ = meteor.transform.DOScale(Vector3.one * Mathf.Max(0.8f, radius), 0.18f)
                .SetEase(Ease.OutQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f),
                cancellationToken: meteor.GetCancellationTokenOnDestroy());
            UnityEngine.Object.Destroy(meteor);
        }

        private async UniTaskVoid SpawnExplosiveCrate(Vector3 position, float radius, int damage)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Relic_Chain_Crate";
            crate.transform.position = position + Vector3.up * 0.45f;
            crate.transform.localScale = Vector3.one * 0.75f;

            Renderer renderer = crate.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.48f, 0.28f, 0.12f);

            _ = crate.transform.DOPunchScale(Vector3.one * 0.18f, 0.35f, 4);
            await UniTask.Delay(TimeSpan.FromSeconds(1f),
                cancellationToken: crate.GetCancellationTokenOnDestroy());

            DealAreaDamage(crate.transform.position, radius, damage);
            _ = crate.transform.DOScale(Vector3.one * radius, 0.15f).SetEase(Ease.OutQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(0.16f),
                cancellationToken: crate.GetCancellationTokenOnDestroy());
            UnityEngine.Object.Destroy(crate);
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

        private void CacheBaseMoveSpeed()
        {
            if (_baseMoveSpeed < 0f)
                _baseMoveSpeed = Mathf.Max(0.01f, _characterStats.MovementSpeed);
        }

        private static string GetCooldownKey(RelicEffectDefinition effect) =>
            $"{effect.TriggerType}_{effect.StatType}_{effect.StatusEffectId}_{effect.EffectPrefabId}";

        private static bool IsEliteOrBoss(EnemyFacade target)
        {
            string targetName = target.name.ToLowerInvariant();
            string configurationName = target.Configuration != null
                ? target.Configuration.name.ToLowerInvariant()
                : string.Empty;

            return targetName.Contains("elite") || targetName.Contains("boss") ||
                   configurationName.Contains("elite") || configurationName.Contains("boss");
        }
    }

    public sealed class RelicChestSpawner
    {
        private readonly ICharacterProvider _characterProvider;
        private readonly RelicChestConfiguration _configuration;
        private readonly LevelsConfiguration _levelsConfiguration;
        private readonly RelicPool _relicPool;
        private readonly RelicManager _relicManager;
        private readonly RelicEventBus _eventBus;
        private readonly DiContainer _container;
        private readonly HashSet<Room> _spawnedRooms = new();
        private readonly List<RelicChest> _activeChests = new();

        public IReadOnlyList<RelicChest> ActiveChests => _activeChests;

        public RelicChestSpawner(ICharacterProvider characterProvider, RelicChestConfiguration configuration,
            LevelsConfiguration levelsConfiguration, RelicPool relicPool, RelicManager relicManager,
            RelicEventBus eventBus, DiContainer container)
        {
            _characterProvider = characterProvider;
            _configuration = configuration;
            _levelsConfiguration = levelsConfiguration;
            _relicPool = relicPool;
            _relicManager = relicManager;
            _eventBus = eventBus;
            _container = container;
        }

        public void SpawnForLevel(LevelView level)
        {
            _spawnedRooms.Clear();
            _activeChests.Clear();
            _eventBus.PublishChestsCleared();

            if (level == null || _configuration.ChestPrefab == null ||
                _configuration.RelicPickupPrefab == null)
                return;

            List<Room> rooms = level.Rooms
                .Where(node => node?.Room?.RoomData is DefaultEnemiesRoomData)
                .Select(node => (Room)node.Room)
                .ToList();
            if (rooms.Count == 0)
                return;

            Shuffle(rooms);
            int chestCount = GetRandomChestCount(rooms.Count);
            if (chestCount <= 0)
                return;

            var excludedRelicIds = new HashSet<string>();
            int spawnedCount = 0;

            for (int index = 0; index < rooms.Count && spawnedCount < chestCount; index++)
            {
                if (rooms[index].RoomData is not DefaultEnemiesRoomData roomData ||
                    _spawnedRooms.Contains(rooms[index]))
                    continue;

                RelicDefinition relic = _relicPool.Roll(_relicManager.ActiveRelics, excludedRelicIds);
                if (relic == null)
                    return;

                excludedRelicIds.Add(relic.Id);
                if (SpawnChest(rooms[index], roomData, relic))
                    spawnedCount++;
            }
        }

        private int GetRandomChestCount(int availableRooms)
        {
            if (availableRooms <= 0)
                return 0;

            int min = Mathf.Min(_configuration.MinChestsPerLevel, _configuration.MaxChestsPerLevel);
            int max = Mathf.Max(_configuration.MinChestsPerLevel, _configuration.MaxChestsPerLevel);

            min = Mathf.Clamp(min, 0, availableRooms);
            max = Mathf.Clamp(max, 0, availableRooms);
            if (max <= min)
                return min;

            return UnityEngine.Random.Range(min, max + 1);
        }

        private bool SpawnChest(Room room, DefaultEnemiesRoomData roomData, RelicDefinition relic)
        {
            if (_spawnedRooms.Contains(room))
                return false;

            if (TryGetGroundPoint(room, out Vector3 groundPoint) == false)
            {
                Debug.LogWarning($"Could not find grounded spawn position for relic chest in {room.name}.");
                return false;
            }

            GameObject chestObject = _container.InstantiatePrefab(_configuration.ChestPrefab,
                groundPoint + Vector3.up * _configuration.ChestSpawnHeight, Quaternion.identity, room.transform);

            RelicChest chest = chestObject.GetComponent<RelicChest>();
            if (chest == null)
                throw new InvalidOperationException(
                    $"{_configuration.ChestPrefab.name} must contain RelicChest component.");

            AlignBottomToGround(chestObject, groundPoint.y);
            _spawnedRooms.Add(room);
            _activeChests.Add(chest);
            chest.Construct(relic, _configuration, _relicManager, _eventBus, _characterProvider,
                _container, roomData, room);
            _eventBus.PublishChestSpawned(roomData, room, chestObject.transform.position);
            return true;
        }

        private bool TryGetGroundPoint(Room room, out Vector3 groundPoint)
        {
            int attempts = Mathf.Max(1, _configuration.ChestSpawnAttempts);
            List<Collider> groundColliders = GetGroundColliders(room);

            if (TryCreateGroundSpawnBounds(groundColliders, out Bounds spawnBounds))
            {
                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    Vector3 candidate = GetRandomSpawnCandidate(spawnBounds);
                    if (TryProjectToGround(room, candidate, out groundPoint) &&
                        IsObstacleFree(groundPoint) &&
                        IsAwayFromDoors(room, groundPoint))
                    {
                        return true;
                    }
                }

                if (TryProjectToGround(room, spawnBounds.center, out groundPoint) &&
                    IsObstacleFree(groundPoint) &&
                    IsAwayFromDoors(room, groundPoint))
                {
                    return true;
                }
            }

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                Vector3 candidate = groundColliders.Count > 0
                    ? GetRandomSpawnCandidate(groundColliders)
                    : GetRandomSpawnCandidate(room);

                if (TryProjectToGround(room, candidate, out groundPoint) &&
                    IsObstacleFree(groundPoint) &&
                    IsAwayFromDoors(room, groundPoint))
                {
                    return true;
                }
            }

            foreach (Collider groundCollider in groundColliders.OrderByDescending(GetHorizontalArea))
            {
                if (TryProjectToGround(room, groundCollider.bounds.center, out groundPoint) &&
                    IsObstacleFree(groundPoint) &&
                    IsAwayFromDoors(room, groundPoint))
                {
                    return true;
                }
            }

            groundPoint = Vector3.zero;
            return false;
        }

        private List<Collider> GetGroundColliders(Room room)
        {
            var groundColliders = new List<Collider>();
            LayerMask groundLayer = GetGroundLayerMask();
            Collider[] colliders = room.GetComponentsInChildren<Collider>(false);

            foreach (Collider collider in colliders)
            {
                if (collider == null || collider.isTrigger ||
                    ContainsLayer(groundLayer, collider.gameObject.layer) == false)
                    continue;

                groundColliders.Add(collider);
            }

            return groundColliders;
        }

        private bool TryCreateGroundSpawnBounds(IReadOnlyList<Collider> groundColliders, out Bounds spawnBounds)
        {
            if (groundColliders.Count == 0)
            {
                spawnBounds = default;
                return false;
            }

            spawnBounds = groundColliders[0].bounds;
            for (int index = 1; index < groundColliders.Count; index++)
                spawnBounds.Encapsulate(groundColliders[index].bounds);

            float padding = CalculateSpawnPadding(spawnBounds);
            if (padding <= 0f)
                return true;

            spawnBounds.Expand(new Vector3(-padding * 2f, 0f, -padding * 2f));
            return spawnBounds.size.x > Mathf.Epsilon && spawnBounds.size.z > Mathf.Epsilon;
        }

        private float CalculateSpawnPadding(Bounds bounds)
        {
            float requestedPadding = Mathf.Max(5f, _configuration.ObstacleCheckRadius * 4f);
            float maxPadding = Mathf.Min(bounds.extents.x, bounds.extents.z) - 0.5f;
            return Mathf.Clamp(requestedPadding, 0f, Mathf.Max(0f, maxPadding));
        }

        private Vector3 GetRandomSpawnCandidate(Bounds bounds) =>
            new(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y,
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z));

        private Vector3 GetRandomSpawnCandidate(IReadOnlyList<Collider> groundColliders)
        {
            Collider groundCollider = groundColliders[UnityEngine.Random.Range(0, groundColliders.Count)];
            Bounds bounds = groundCollider.bounds;
            float margin = Mathf.Max(0.05f, _configuration.ObstacleCheckRadius);
            float minX = bounds.min.x + margin;
            float maxX = bounds.max.x - margin;
            float minZ = bounds.min.z + margin;
            float maxZ = bounds.max.z - margin;

            if (minX > maxX)
            {
                minX = bounds.min.x;
                maxX = bounds.max.x;
            }

            if (minZ > maxZ)
            {
                minZ = bounds.min.z;
                maxZ = bounds.max.z;
            }

            return new Vector3(
                UnityEngine.Random.Range(minX, maxX),
                bounds.max.y,
                UnityEngine.Random.Range(minZ, maxZ));
        }

        private Vector3 GetRandomSpawnCandidate(Room room)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle *
                             Mathf.Max(0f, _configuration.ChestRoomOffsetRadius);
            Vector3 localPosition = new(offset.x, 0f, offset.y);
            return room.transform.TransformPoint(localPosition);
        }

        private bool TryProjectToGround(Room room, Vector3 position, out Vector3 groundPoint)
        {
            Vector3 rayOrigin = position + Vector3.up * _configuration.GroundRayStartHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    _configuration.GroundRayDistance, GetGroundLayerMask(), QueryTriggerInteraction.Ignore) == false)
            {
                groundPoint = Vector3.zero;
                return false;
            }

            if (hit.collider == null || hit.collider.transform.IsChildOf(room.transform) == false ||
                hit.normal.y < 0.75f)
            {
                groundPoint = Vector3.zero;
                return false;
            }

            groundPoint = hit.point;
            return true;
        }

        private bool IsObstacleFree(Vector3 position)
        {
            if (_levelsConfiguration.ObstacleLayer.value == 0 || _configuration.ObstacleCheckRadius <= 0f)
                return true;

            Vector3 checkPosition = position + Vector3.up * _configuration.ObstacleCheckHeight;
            Collider[] colliders = Physics.OverlapSphere(checkPosition, _configuration.ObstacleCheckRadius,
                _levelsConfiguration.ObstacleLayer, QueryTriggerInteraction.Ignore);
            return colliders.Length == 0;
        }

        private bool IsAwayFromDoors(Room room, Vector3 position)
        {
            const float MinDoorDistance = 8f;

            if (room.RoomData?.RoomDoors == null)
                return true;

            float minDoorDistanceSqr = MinDoorDistance * MinDoorDistance;
            foreach (RoomDoor door in room.RoomData.RoomDoors)
            {
                if (door == null)
                    continue;

                Vector3 offset = door.transform.position - position;
                offset.y = 0f;
                if (offset.sqrMagnitude < minDoorDistanceSqr)
                    return false;
            }

            return true;
        }

        private LayerMask GetGroundLayerMask() =>
            _levelsConfiguration.GroundLayer.value == 0
                ? Physics.DefaultRaycastLayers
                : _levelsConfiguration.GroundLayer;

        private static bool ContainsLayer(LayerMask layerMask, int layer) =>
            (layerMask.value & (1 << layer)) != 0;

        private static float GetHorizontalArea(Collider collider) =>
            collider.bounds.size.x * collider.bounds.size.z;

        private static void AlignBottomToGround(GameObject chestObject, float groundY)
        {
            Renderer[] renderers = chestObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);

            float yOffset = groundY - bounds.min.y;
            chestObject.transform.position += Vector3.up * yOffset;
        }

        private static void Shuffle<T>(IList<T> items)
        {
            for (int index = 0; index < items.Count - 1; index++)
            {
                int swapIndex = UnityEngine.Random.Range(index, items.Count);
                (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
            }
        }
    }

    public sealed class RelicInventoryViewService : IDisposable
    {
        private readonly RelicManager _relicManager;
        private readonly List<GameObject> _slotObjects = new();

        private RectTransform _root;
        private RectTransform _tooltipRoot;
        private TMP_Text _tooltipText;

        public RelicInventoryViewService(RelicManager relicManager)
        {
            _relicManager = relicManager;
            _relicManager.Changed += Refresh;
        }

        public void Attach(CharacterPanel panel)
        {
            if (panel == null || _root != null)
                return;

            _root = CreateRoot(panel.transform);
            _tooltipRoot = CreateTooltip(panel.transform, out _tooltipText);
            Refresh();
        }

        public void Detach()
        {
            if (_root != null)
                UnityEngine.Object.Destroy(_root.gameObject);

            if (_tooltipRoot != null)
                UnityEngine.Object.Destroy(_tooltipRoot.gameObject);

            _root = null;
            _tooltipRoot = null;
            _tooltipText = null;
            _slotObjects.Clear();
        }

        public void Dispose()
        {
            _relicManager.Changed -= Refresh;
            Detach();
        }

        private void Refresh()
        {
            if (_root == null)
                return;

            foreach (GameObject slot in _slotObjects)
                UnityEngine.Object.Destroy(slot);
            _slotObjects.Clear();

            foreach (RelicRuntimeState state in _relicManager.ActiveRelics)
                _slotObjects.Add(CreateSlot(_root, state));
        }

        private GameObject CreateSlot(RectTransform root, RelicRuntimeState state)
        {
            GameObject slot = new($"RelicSlot_{state.Definition.Id}", typeof(RectTransform),
                typeof(Image), typeof(RelicTooltipTrigger));
            slot.transform.SetParent(root, false);

            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(48f, 48f);

            Image border = slot.GetComponent<Image>();
            border.color = GetRarityColor(state.Definition.Rarity);

            GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(slot.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = state.Definition.Icon;
            icon.preserveAspect = true;

            GameObject countObject = new("StackText", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObject.transform.SetParent(slot.transform, false);
            RectTransform countRect = countObject.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 0f);
            countRect.anchorMax = new Vector2(1f, 0.45f);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
            TMP_Text countText = countObject.GetComponent<TMP_Text>();
            countText.text = state.IsBroken ? "X" : state.StackCount.ToString();
            countText.fontSize = 17f;
            countText.alignment = TextAlignmentOptions.BottomRight;
            countText.color = state.IsBroken ? Color.red : Color.white;

            slot.GetComponent<RelicTooltipTrigger>().Construct(state, ShowTooltip, HideTooltip);
            return slot;
        }

        private void ShowTooltip(RelicRuntimeState state, Vector2 screenPosition)
        {
            if (_tooltipRoot == null)
                return;

            _tooltipRoot.gameObject.SetActive(true);
            _tooltipRoot.position = screenPosition + new Vector2(16f, -16f);
            _tooltipText.text =
                $"{state.Definition.DisplayName}\n" +
                $"{state.Definition.Rarity} | x{state.StackCount}\n" +
                $"{state.Definition.Description}\n" +
                $"{string.Join(", ", state.Definition.Tags)}" +
                (state.IsBroken ? "\nBROKEN" : string.Empty);
        }

        private void HideTooltip()
        {
            if (_tooltipRoot != null)
                _tooltipRoot.gameObject.SetActive(false);
        }

        private static RectTransform CreateRoot(Transform parent)
        {
            GameObject rootObject = new("RelicInventory", typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            rootObject.transform.SetParent(parent, false);

            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(18f, -88f);
            root.sizeDelta = new Vector2(520f, 52f);

            HorizontalLayoutGroup layout = rootObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return root;
        }

        private static RectTransform CreateTooltip(Transform parent, out TMP_Text text)
        {
            GameObject tooltip = new("RelicTooltip", typeof(RectTransform), typeof(Image));
            tooltip.transform.SetParent(parent, false);
            RectTransform rect = tooltip.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(360f, 124f);
            tooltip.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.06f, 0.92f);

            GameObject textObject = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(tooltip.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);
            text = textObject.GetComponent<TMP_Text>();
            text.fontSize = 15f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            tooltip.SetActive(false);
            return rect;
        }

        private static Color GetRarityColor(RelicRarity rarity) =>
            rarity switch
            {
                RelicRarity.Common => new Color(0.28f, 0.85f, 0.28f),
                RelicRarity.Uncommon => new Color(0.2f, 0.55f, 1f),
                RelicRarity.Rare => new Color(0.85f, 0.25f, 1f),
                RelicRarity.Legendary => new Color(1f, 0.75f, 0.12f),
                _ => Color.white
            };
    }

    public sealed class RelicTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RelicRuntimeState _state;
        private Action<RelicRuntimeState, Vector2> _show;
        private Action _hide;

        public void Construct(RelicRuntimeState state, Action<RelicRuntimeState, Vector2> show,
            Action hide)
        {
            _state = state;
            _show = show;
            _hide = hide;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            _show?.Invoke(_state, eventData.position);

        public void OnPointerExit(PointerEventData eventData) =>
            _hide?.Invoke();
    }
}
