using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public sealed partial class RelicManager
    {
        private const string CautiousSlugId = "cautious_slug";
        private const string RepulsionArmorPlateId = "repulsion_armor_plate";
        private const string FocusCrystalId = "focus_crystal";
        private const string StunGrenadeId = "stun_grenade";
        private const string CrowbarId = "crowbar";
        private const string TopazBroochId = "topaz_brooch";
        private const string HarvestersScytheId = "harvesters_scythe";
        private const string PredatoryInstinctsId = "predatory_instincts";
        private const string UkuleleId = "ukulele";
        private const string BrilliantBehemothId = "brilliant_behemoth";
        private const string SoulboundCatalystId = "soulbound_catalyst";
        private const string UnstableTeslaCoilId = "unstable_tesla_coil";
        private const string LuckyRerollId = "lucky_reroll";
        private const string ShapedGlassDamageId = "shaped_glass_damage";
        private const string LumpOfCoalId = "lump_of_coal";
        private const string SpiderBiteId = "spider_bite";
        private const string PiggyBankId = "piggy_bank";
        private const string CharmOfTheVampireId = "charm_of_the_vampire";
        private const string ToughLoveId = "tough_love";
        private const string HabitId = "habit";
        private const string WhoreOfBabylonId = "whore_of_babylon";
        private const string MoneyEqualsPowerId = "money_equals_power";
        private const string CancerId = "cancer";
        private const string FireMindBurnId = "fire_mind_burn";
        private const string FireMindExplosionId = "fire_mind_explosion";
        private const string HolyMantleId = "holy_mantle";
        private const string WaferId = "wafer";
        private const string StopWatchId = "stop_watch";
        private const string VenomBladeId = "venom_blade";
        private const string HotDogId = "hot_dog";
        private const string CactusId = "cactus";
        private const string GoldenSneakersId = "golden_sneakers";
        private const string SpikyShieldId = "spiky_shield";
        private const string IronHammerId = "iron_hammer";
        private const string VoodooDollId = "voodoo_doll";
        private const string TurboSkatesId = "turbo_skates";
        private const string OverpoweredChaliceId = "overpowered_chalice";

        private const string HolyMantleReadyKey = "holy_mantle_ready";
        private const string CancerActiveKey = "cancer_active";
        private const string VampireKillsKey = "vampire_kills";
        private const string PredatoryStacksKey = "predatory_stacks";
        private const string TeslaCycleStartKey = "tesla_cycle_start";
        private const string TeslaNextZapKey = "tesla_next_zap";
        private const string SpikyShieldAppliedKey = "spiky_shield_applied";
        private const string TurboSkatesAppliedKey = "turbo_skates_applied";
        private const float DefaultTeslaDamage = 12f;

        private readonly Dictionary<string, RelicRuntimeState> _temporaryModifierOwners = new();
        private readonly Dictionary<EnemyFacade, int> _activeVenomBladePoisons = new();
        private int _temporaryModifierSequence;
        private float _lastDamageTakenTime;
        private float _lastStopWatchMultiplier = 1f;
        private float _nextStopWatchRefreshTime;

        public bool TryBlockIncomingDamage(CharacterFacade victim)
        {
            if (victim == null)
                return false;

            foreach (RelicRuntimeState state in _activeRelics)
            {
                if (state.IsBroken || HasEffect(state, HolyMantleId) == false)
                    continue;

                state.CustomCounters.TryGetValue(HolyMantleReadyKey, out float ready);
                if (ready <= 0f) 
                    continue;

                state.CustomCounters[HolyMantleReadyKey] = 0f;
                Changed?.Invoke();
                return true;
            }

            return false;
        }

        public int ModifyIncomingDamage(CharacterFacade victim, int damage)
        {
            if (victim == null || damage <= 0)
                return damage;

            float flatReduction = 0f;
            int damageCap = int.MaxValue;

            foreach (RelicRuntimeState state in _activeRelics)
            {
                if (state.IsBroken)
                    continue;

                foreach (RelicEffectDefinition effect in state.Definition.Effects ??
                                                         Array.Empty<RelicEffectDefinition>())
                {
                    string effectId = GetSpecialEffectId(effect);
                    if (effectId == RepulsionArmorPlateId)
                    {
                        flatReduction += Mathf.Max(0f, effect.Value) * state.StackCount;
                        continue;
                    }

                    if (effectId == WaferId)
                    {
                        int cap = Mathf.Max(1,
                            Mathf.CeilToInt(victim.HealthSystem.MaxHealth * Mathf.Clamp01(effect.Value)));
                        damageCap = Mathf.Min(damageCap, cap);
                        continue;
                    }

                    if (effectId != CancerId)
                        continue;

                    state.CustomCounters.TryGetValue(CancerActiveKey, out float cancerActive);
                    if (cancerActive <= 0f)
                        continue;

                    int capAfterFirstHit = Mathf.Max(1,
                        Mathf.CeilToInt(victim.HealthSystem.MaxHealth * Mathf.Clamp01(effect.Value)));
                    damageCap = Mathf.Min(damageCap, capAfterFirstHit);
                }
            }

            int reducedDamage = Mathf.Max(1, damage - Mathf.RoundToInt(flatReduction));
            return Mathf.Min(reducedDamage, damageCap);
        }

        private void TickSpecialRelics(CharacterFacade character)
        {
            float stopWatchMultiplier = 1f;

            foreach (RelicRuntimeState state in _activeRelics.ToArray())
            {
                if (state.IsBroken)
                    continue;

                foreach (RelicEffectDefinition effect in state.Definition.Effects ??
                                                         Array.Empty<RelicEffectDefinition>())
                {
                    switch (GetSpecialEffectId(effect))
                    {
                        case CautiousSlugId:
                            TickCautiousSlug(state, effect, character);
                            break;
                        case UnstableTeslaCoilId:
                            TickUnstableTesla(state, effect, character);
                            break;
                        case WhoreOfBabylonId:
                            TickWhoreOfBabylon(state, effect, character);
                            break;
                        case SpikyShieldId:
                            TickSpikyShield(state, effect);
                            break;
                        case TurboSkatesId:
                            TickTurboSkates(state, effect, character);
                            break;
                        case StopWatchId:
                            stopWatchMultiplier = Mathf.Min(stopWatchMultiplier,
                                Mathf.Pow(Mathf.Clamp(effect.Value, 0.05f, 1f), state.StackCount));
                            break;
                    }
                }
            }

            if (Mathf.Approximately(stopWatchMultiplier, _lastStopWatchMultiplier) == false ||
                stopWatchMultiplier < 1f && Time.time >= _nextStopWatchRefreshTime)
            {
                ApplyStopWatchSlow(stopWatchMultiplier);
                _lastStopWatchMultiplier = stopWatchMultiplier;
                _nextStopWatchRefreshTime = Time.time + 0.5f;
            }
        }

        private void TickCautiousSlug(RelicRuntimeState state, RelicEffectDefinition effect,
            CharacterFacade character)
        {
            if (Time.time - _lastDamageTakenTime < Mathf.Max(0f, effect.Duration))
                return;

            HealCharacter(character, Mathf.Max(0f, effect.Value) * state.StackCount * Time.deltaTime);
        }

        private void TickUnstableTesla(RelicRuntimeState state, RelicEffectDefinition effect,
            CharacterFacade character)
        {
            state.CustomCounters.TryGetValue(TeslaCycleStartKey, out float cycleStart);
            if (cycleStart <= 0f)
            {
                cycleStart = Time.time;
                state.CustomCounters[TeslaCycleStartKey] = cycleStart;
            }

            float activeDuration = Mathf.Max(0.1f, effect.Duration);
            float cycleDuration = activeDuration * 2f;
            if ((Time.time - cycleStart) % cycleDuration >= activeDuration)
                return;

            state.CooldownTimers.TryGetValue(TeslaNextZapKey, out float nextZap);
            if (Time.time < nextZap)
                return;

            state.CooldownTimers[TeslaNextZapKey] = Time.time + Mathf.Max(0.1f, effect.Cooldown);
            int targetCount = Mathf.Max(1, Mathf.RoundToInt(effect.Cap)) +
                              Mathf.Max(0, state.StackCount - 1) * 2;
            int damage = Mathf.Max(1,
                Mathf.RoundToInt(DefaultTeslaDamage * Mathf.Max(0.1f, effect.Value) * state.StackCount));
            DealNearestDamage(character.transform.position, Mathf.Max(1f, effect.Radius), damage,
                targetCount, UnstableTeslaCoilId, null);
        }

        private void TickWhoreOfBabylon(RelicRuntimeState state, RelicEffectDefinition effect,
            CharacterFacade character)
        {
            float healthRatio = character.HealthSystem.MaxHealth > 0f
                ? character.HealthSystem.CurrentHealth / character.HealthSystem.MaxHealth
                : 1f;
            bool shouldBeActive = healthRatio <= Mathf.Clamp01(effect.Duration);
            string activeKey = $"{WhoreOfBabylonId}_active";
            state.CustomCounters.TryGetValue(activeKey, out float wasActiveValue);
            bool wasActive = wasActiveValue > 0f;
            if (wasActive == shouldBeActive)
                return;

            string sourceId = GetWhoreModifierSource(state);
            _statModifierLayer.RemoveModifiers(sourceId);
            state.CustomCounters[activeKey] = shouldBeActive ? 1f : 0f;

            if (shouldBeActive)
            {
                _statModifierLayer.AddModifier(sourceId, StatType.Damage,
                    effect.Value * state.StackCount, CharacterStatModifierStackingType.AdditivePercent);
                _statModifierLayer.AddModifier(sourceId, StatType.MovementSpeed,
                    effect.BossValue * state.StackCount, CharacterStatModifierStackingType.PercentOfBase);
            }
        }

        private void TickSpikyShield(RelicRuntimeState state, RelicEffectDefinition effect)
        {
            float thorns = Mathf.Max(0f, _characterStats.Armor) *
                           Mathf.Max(0f, effect.Value) * state.StackCount;
            UpdateDynamicModifier(state, SpikyShieldId, SpikyShieldAppliedKey,
                StatType.ThornsDamage, thorns, CharacterStatModifierStackingType.Flat);
        }

        private void TickTurboSkates(RelicRuntimeState state, RelicEffectDefinition effect,
            CharacterFacade character)
        {
            Vector3 velocity = character.Rigidbody.linearVelocity;
            velocity.y = 0f;
            float speedRatio = Mathf.Clamp01(velocity.magnitude /
                                             Mathf.Max(0.1f, _characterStats.MovementSpeed));
            float attackSpeedBonus = Mathf.Round(
                Mathf.Max(0f, effect.Value) * state.StackCount * speedRatio * 1000f) / 1000f;
            UpdateDynamicModifier(state, TurboSkatesId, TurboSkatesAppliedKey,
                StatType.AttackSpeed, attackSpeedBonus,
                CharacterStatModifierStackingType.AdditivePercent);
        }

        private void UpdateDynamicModifier(RelicRuntimeState state, string effectId, string counterKey,
            StatType stat, float value, CharacterStatModifierStackingType stackingType)
        {
            state.CustomCounters.TryGetValue(counterKey, out float appliedValue);
            if (Mathf.Approximately(appliedValue, value))
                return;

            string sourceId = GetSpecialModifierSource(state, effectId);
            _statModifierLayer.RemoveModifiers(sourceId);
            state.CustomCounters[counterKey] = value;

            if (value > 0f)
                _statModifierLayer.AddModifier(sourceId, stat, value, stackingType);
        }

        private static void ApplyStopWatchSlow(float multiplier)
        {
            EnemyFacade[] enemies = UnityEngine.Object.FindObjectsByType<EnemyFacade>(FindObjectsSortMode.None);
            foreach (EnemyFacade enemy in enemies)
            {
                if (enemy != null && enemy.IsDead == false)
                    enemy.SetPersistentRelicSlow(multiplier);
            }
        }

        private int ModifySpecialOutgoingDamage(int damage, EnemyFacade target)
        {
            if (damage <= 0 || target == null)
                return damage;

            float additiveMultiplier = 1f;
            float multiplicativeMultiplier = 1f;
            CharacterFacade character = _characterProvider.CharacterFacade;
            float distance = character != null
                ? Vector3.Distance(character.transform.position, target.transform.position)
                : 0f;

            foreach (RelicRuntimeState state in _activeRelics)
            {
                if (state.IsBroken)
                    continue;

                foreach (RelicEffectDefinition effect in state.Definition.Effects ??
                                                         Array.Empty<RelicEffectDefinition>())
                {
                    switch (GetSpecialEffectId(effect))
                    {
                        case FocusCrystalId when distance <= Mathf.Max(0f, effect.Radius):
                            additiveMultiplier += Mathf.Max(0f, effect.Value) * state.StackCount;
                            break;
                        case CrowbarId when target.HealthSystem.MaxHealth > 0f &&
                                            target.HealthSystem.CurrentHealth /
                                            target.HealthSystem.MaxHealth >= effect.Duration:
                            additiveMultiplier += Mathf.Max(0f, effect.Value) * state.StackCount;
                            break;
                        case LumpOfCoalId:
                            additiveMultiplier += Mathf.Min(Mathf.Max(0f, effect.Cap),
                                distance * Mathf.Max(0f, effect.Value) * state.StackCount);
                            break;
                        case MoneyEqualsPowerId:
                            additiveMultiplier += Mathf.Min(Mathf.Max(0f, effect.Cap),
                                Mathf.Max(0, _characterWallet.Gold.Count) * Mathf.Max(0f, effect.Value) *
                                state.StackCount);
                            break;
                        case ToughLoveId:
                            float chance = Mathf.Clamp01(effect.Chance * state.StackCount +
                                                         Mathf.Max(0f, _characterStats.Luck) * 0.01f);
                            if (RollSpecialChance(chance))
                                multiplicativeMultiplier *= 1f + Mathf.Max(0f, effect.Value);
                            break;
                        case IronHammerId:
                            int procCount = RollIronHammerProcCount(state, effect);
                            if (procCount > 0)
                            {
                                float bonkMultiplier = Mathf.Max(1f, effect.Value +
                                    Mathf.Max(0, state.StackCount - 1) * Mathf.Max(0f, effect.Duration));
                                multiplicativeMultiplier *= 1f + procCount * (bonkMultiplier - 1f);
                            }
                            break;
                        case ShapedGlassDamageId:
                            multiplicativeMultiplier *= Mathf.Pow(1f + Mathf.Max(0f, effect.Value),
                                state.StackCount);
                            break;
                    }
                }
            }

            return Mathf.Max(1,
                Mathf.RoundToInt(damage * additiveMultiplier * multiplicativeMultiplier));
        }

        private bool TryApplySpecialEffect(RelicRuntimeState state, RelicEffectDefinition effect,
            RelicTriggerType triggerType, object context)
        {
            string effectId = GetSpecialEffectId(effect);
            if (string.IsNullOrEmpty(effectId))
                return false;

            if (context is RelicHitEvent hitEvent)
            {
                if (triggerType == RelicTriggerType.OnHit)
                {
                    switch (effectId)
                    {
                        case StunGrenadeId:
                            hitEvent.Target?.ApplyRelicStun(effect.Duration);
                            return true;
                        case SpiderBiteId:
                            hitEvent.Target?.ApplyRelicSlow(effect.Value,
                                effect.Duration * state.StackCount);
                            return true;
                        case UkuleleId:
                            DealNearestDamage(hitEvent.HitPosition, effect.Radius,
                                Mathf.Max(1, Mathf.RoundToInt(hitEvent.Damage * effect.Value * state.StackCount)),
                                Mathf.Max(1, Mathf.RoundToInt(effect.Cap)), UkuleleId, hitEvent.Target);
                            return true;
                        case BrilliantBehemothId:
                        case FireMindExplosionId:
                            DealAreaDamage(hitEvent.HitPosition, Mathf.Max(1f, effect.Radius),
                                Mathf.Max(1, Mathf.RoundToInt(hitEvent.Damage * effect.Value * state.StackCount)),
                                effectId);
                            return true;
                        case FireMindBurnId:
                            ApplyFireMindBurn(state, effect, hitEvent).Forget();
                            return true;
                        case VenomBladeId:
                            ApplyVenomBladePoison(state, effect, hitEvent).Forget();
                            return true;
                        case VoodooDollId:
                            ApplyVoodooDollSelfDamage(state, effect, hitEvent.Attacker);
                            return true;
                    }
                }

                if (triggerType == RelicTriggerType.OnCrit)
                {
                    if (effectId == HarvestersScytheId)
                    {
                        HealCharacter(hitEvent.Attacker, effect.Value * state.StackCount);
                        return true;
                    }

                    if (effectId == PredatoryInstinctsId)
                    {
                        AddPredatoryInstinctsStack(state, effect).Forget();
                        return true;
                    }
                }
            }

            if (triggerType == RelicTriggerType.OnKill && context is RelicKillEvent killEvent)
            {
                switch (effectId)
                {
                    case TopazBroochId:
                        killEvent.Killer?.ShieldSystem.AddTemporaryShield(effect.Value * state.StackCount);
                        return true;
                    case SoulboundCatalystId:
                        killEvent.Killer?.CharacterAbilitySystem.ReduceActiveCooldowns(
                            effect.Value * state.StackCount);
                        return true;
                    case CharmOfTheVampireId:
                        ApplyVampireKill(state, effect, killEvent.Killer);
                        return true;
                    case HotDogId:
                        ApplyHotDogHeal(state, effect, killEvent.Killer);
                        return true;
                }
            }

            if (triggerType == RelicTriggerType.OnDamageTaken &&
                context is RelicDamageTakenEvent damageTakenEvent)
            {
                switch (effectId)
                {
                    case PiggyBankId:
                        _characterWallet.Gold.Add(Mathf.Max(1,
                            Mathf.RoundToInt(effect.Value * state.StackCount)));
                        return true;
                    case HabitId:
                        damageTakenEvent.Victim?.CharacterAbilitySystem.ReduceActiveCooldowns(
                            effect.Value * state.StackCount);
                        return true;
                    case CancerId:
                        state.CustomCounters[CancerActiveKey] = 1f;
                        return true;
                    case CactusId:
                        ApplyCactusRetaliation(state, effect, damageTakenEvent);
                        return true;
                }
            }

            if (triggerType == RelicTriggerType.OnRoomCompleted &&
                context is DefaultEnemiesRoomData && effectId == GoldenSneakersId)
            {
                _characterWallet.Gold.Add(Mathf.Max(1,
                    Mathf.RoundToInt(effect.Value * state.StackCount)));
                return true;
            }

            return triggerType == RelicTriggerType.OnRoomStart && effectId == HolyMantleId;
        }

        private void ApplyHotDogHeal(RelicRuntimeState state, RelicEffectDefinition effect,
            CharacterFacade character)
        {
            if (character == null)
                return;

            float percentHealing = character.HealthSystem.MaxHealth * Mathf.Max(0f, effect.Value);
            float flatHealing = Mathf.Max(0f, effect.Cap) +
                                Mathf.Max(0f, effect.Duration) * state.StackCount;
            HealCharacter(character, Mathf.Max(percentHealing, flatHealing));
        }

        private void ApplyCactusRetaliation(RelicRuntimeState state, RelicEffectDefinition effect,
            RelicDamageTakenEvent damageTakenEvent)
        {
            CharacterFacade victim = damageTakenEvent.Victim;
            if (victim == null)
                return;

            float damageMultiplier = 1f + Mathf.Max(0f, _characterStats.DamageInPercent) * 0.01f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(
                (Mathf.Max(0f, _characterStats.ThornsDamage) +
                 Mathf.Max(0f, effect.Value) * state.StackCount) * damageMultiplier));
            DealAreaDamage(victim.transform.position, Mathf.Max(1f, effect.Radius), damage, CactusId);
        }

        private void ApplyVoodooDollSelfDamage(RelicRuntimeState state, RelicEffectDefinition effect,
            CharacterFacade character)
        {
            if (character == null || character.HealthSystem.CurrentHealth <= 1f)
                return;

            float healthBeforeDamage = character.HealthSystem.CurrentHealth;
            float selfDamage = Mathf.Max(1f, effect.Value);
            float healthAfterDamage = Mathf.Max(1f, healthBeforeDamage - selfDamage);
            int appliedDamage = Mathf.CeilToInt(healthBeforeDamage - healthAfterDamage);
            if (appliedDamage <= 0)
                return;

            character.HealthSystem.SetCurrentHealth(healthAfterDamage);
            character.DamageEffectSystem.DealDamage();
            _eventBus.PublishDamageTaken(new RelicDamageTakenEvent(character, null,
                appliedDamage, VoodooDollId));
        }

        private async UniTaskVoid ApplyVenomBladePoison(RelicRuntimeState state,
            RelicEffectDefinition effect, RelicHitEvent hitEvent)
        {
            EnemyFacade target = hitEvent.Target;
            if (target == null || target.IsDead)
                return;

            _activeVenomBladePoisons.TryGetValue(target, out int activeStacks);
            int stackCap = Mathf.Max(1, Mathf.RoundToInt(effect.Cap));
            if (activeStacks >= stackCap)
                return;

            _activeVenomBladePoisons[target] = activeStacks + 1;
            CancellationTokenSource poisonCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _disposeCancellation.Token, target.GetCancellationTokenOnDestroy());
            try
            {
                int ticks = Mathf.Max(1, Mathf.RoundToInt(effect.Duration));
                int tickDamage = Mathf.Max(1, Mathf.RoundToInt(
                    hitEvent.Damage * Mathf.Max(0f, effect.Value) * state.StackCount));

                for (int tickIndex = 0; tickIndex < ticks; tickIndex++)
                {
                    bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(1f),
                            cancellationToken: poisonCancellation.Token)
                        .SuppressCancellationThrow();
                    if (cancelled || target == null || target.IsDead)
                        return;

                    int appliedDamage = target.HealthSystem.GetDamage(tickDamage);
                    if (appliedDamage <= 0)
                        continue;

                    target.EffectsSystem.DealDamage(0.04f);
                    PublishRelicKillIfDead(target, VenomBladeId, appliedDamage);
                }
            }
            finally
            {
                poisonCancellation.Dispose();
                if (_activeVenomBladePoisons.TryGetValue(target, out activeStacks))
                {
                    if (activeStacks <= 1)
                        _activeVenomBladePoisons.Remove(target);
                    else
                        _activeVenomBladePoisons[target] = activeStacks - 1;
                }
            }
        }

        private void ApplyVampireKill(RelicRuntimeState state, RelicEffectDefinition effect,
            CharacterFacade character)
        {
            state.CustomCounters.TryGetValue(VampireKillsKey, out float killCount);
            killCount += 1f;
            int threshold = Mathf.Max(1, Mathf.RoundToInt(effect.Cap));

            if (killCount >= threshold)
            {
                killCount -= threshold;
                float healing = character != null
                    ? character.HealthSystem.MaxHealth * Mathf.Max(0f, effect.Value) * state.StackCount
                    : 0f;
                HealCharacter(character, healing);
            }

            state.CustomCounters[VampireKillsKey] = killCount;
        }

        private async UniTaskVoid ApplyFireMindBurn(RelicRuntimeState state,
            RelicEffectDefinition effect, RelicHitEvent hitEvent)
        {
            EnemyFacade target = hitEvent.Target;
            if (target == null || target.IsDead)
                return;

            int ticks = Mathf.Max(1, Mathf.RoundToInt(effect.Duration));
            int totalBurnDamage = Mathf.Max(1,
                Mathf.RoundToInt(hitEvent.Damage * Mathf.Max(0f, effect.Value) * state.StackCount));

            for (int index = 0; index < ticks; index++)
            {
                bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(1f),
                        cancellationToken: target.GetCancellationTokenOnDestroy())
                    .SuppressCancellationThrow();
                if (cancelled || target == null || target.IsDead)
                    return;

                int tickDamage = totalBurnDamage / ticks + (index < totalBurnDamage % ticks ? 1 : 0);
                if (tickDamage <= 0)
                    continue;

                int appliedDamage = target.HealthSystem.GetDamage(tickDamage);
                if (appliedDamage <= 0)
                    continue;

                target.EffectsSystem.DealDamage(0.04f);
                PublishRelicKillIfDead(target, FireMindBurnId, appliedDamage);
            }
        }

        private async UniTaskVoid AddPredatoryInstinctsStack(RelicRuntimeState state,
            RelicEffectDefinition effect)
        {
            state.CustomCounters.TryGetValue(PredatoryStacksKey, out float currentStacks);
            int stackCap = Mathf.Max(1, Mathf.RoundToInt(effect.Cap)) +
                           Mathf.Max(0, state.StackCount - 1) * 2;
            if (currentStacks >= stackCap)
                return;

            state.CustomCounters[PredatoryStacksKey] = currentStacks + 1f;
            string sourceId = $"relic-temporary:{state.Definition.Id}:{++_temporaryModifierSequence}";
            _temporaryModifierOwners[sourceId] = state;
            _statModifierLayer.AddModifier(sourceId, StatType.AttackSpeed, effect.Value,
                CharacterStatModifierStackingType.AdditivePercent);

            await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0.05f, effect.Duration)),
                    cancellationToken: _disposeCancellation.Token)
                .SuppressCancellationThrow();

            _statModifierLayer.RemoveModifiers(sourceId);
            _temporaryModifierOwners.Remove(sourceId);
            state.CustomCounters.TryGetValue(PredatoryStacksKey, out currentStacks);
            state.CustomCounters[PredatoryStacksKey] = Mathf.Max(0f, currentStacks - 1f);
        }

        private void DealNearestDamage(Vector3 center, float radius, int damage, int targetCount,
            string sourceId, EnemyFacade excludedTarget)
        {
            float radiusSquared = radius * radius;
            EnemyFacade[] targets = UnityEngine.Object.FindObjectsByType<EnemyFacade>(FindObjectsSortMode.None)
                .Where(enemy => enemy != null && enemy != excludedTarget && enemy.IsDead == false &&
                                (enemy.transform.position - center).sqrMagnitude <= radiusSquared)
                .OrderBy(enemy => (enemy.transform.position - center).sqrMagnitude)
                .Take(Mathf.Max(1, targetCount))
                .ToArray();

            foreach (EnemyFacade target in targets)
            {
                int appliedDamage = target.HealthSystem.GetDamage(damage);
                if (appliedDamage <= 0)
                    continue;

                target.EffectsSystem.DealDamage(0.05f);
                PublishRelicKillIfDead(target, sourceId, appliedDamage);
            }
        }

        private void HealCharacter(CharacterFacade character, float amount)
        {
            if (character == null || amount <= 0f)
                return;

            float healed = character.HealthSystem.IncreaseCurrentHealth(amount);
            if (healed > 0f)
                _eventBus.PublishHeal(new RelicHealEvent(character, healed));
        }

        private int RollTriggerEffect(RelicRuntimeState state, RelicEffectDefinition effect,
            RelicTriggerType triggerType)
        {
            float chance = GetEffectChance(state, effect);
            int chaliceStacks = GetOverpoweredChaliceStackCount();
            bool supportsChalice = triggerType == RelicTriggerType.OnHit && chaliceStacks > 0 &&
                                effect.Chance > 0f && effect.Chance < 1f &&
                                GetSpecialEffectId(effect) != VoodooDollId;
            if (supportsChalice == false)
                return RollEffect(state, effect) ? 1 : 0;

            string cooldownKey = GetCooldownKey(effect);
            if (state.CooldownTimers.TryGetValue(cooldownKey, out float readyTime) &&
                Time.time < readyTime)
                return 0;

            int procCount = 0;
            int rollCount = 1 + chaliceStacks;
            for (int rollIndex = 0; rollIndex < rollCount; rollIndex++)
            {
                if (RollSpecialChance(chance))
                    procCount++;
            }

            if (procCount > 0 && effect.Cooldown > 0f)
                state.CooldownTimers[cooldownKey] = Time.time + effect.Cooldown;

            return procCount;
        }

        private int RollIronHammerProcCount(RelicRuntimeState state, RelicEffectDefinition effect)
        {
            float chance = GetEffectChance(state, effect);
            int procCount = 0;
            int rollCount = 1 + GetOverpoweredChaliceStackCount();
            for (int rollIndex = 0; rollIndex < rollCount; rollIndex++)
            {
                if (RollSpecialChance(chance))
                    procCount++;
            }

            return procCount;
        }

        private int GetOverpoweredChaliceStackCount()
        {
            int stacks = 0;
            foreach (RelicRuntimeState state in _activeRelics)
            {
                if (state.IsBroken == false && HasEffect(state, OverpoweredChaliceId))
                    stacks += state.StackCount;
            }

            return stacks;
        }

        private static float GetEffectChance(RelicRuntimeState state, RelicEffectDefinition effect)
        {
            string effectId = GetSpecialEffectId(effect);
            if (effectId is HotDogId or IronHammerId)
            {
                return Mathf.Clamp01(Mathf.Max(0f, effect.Chance) +
                                     Mathf.Max(0, state.StackCount - 1) *
                                     Mathf.Max(0f, effect.BossValue));
            }

            return effect.GetChance(state.StackCount);
        }

        private bool RollSpecialChance(float chance)
        {
            chance = Mathf.Clamp01(chance);
            if (UnityEngine.Random.value <= chance)
                return true;

            return HasActiveEffect(LuckyRerollId) && UnityEngine.Random.value <= chance;
        }

        private bool HasActiveEffect(string effectId) =>
            _activeRelics.Any(state => state.IsBroken == false && HasEffect(state, effectId));

        private static bool HasEffect(RelicRuntimeState state, string effectId) =>
            state.Definition.Effects?.Any(effect => GetSpecialEffectId(effect) == effectId) == true;

        private static string GetSpecialEffectId(RelicEffectDefinition effect)
        {
            if (string.IsNullOrWhiteSpace(effect.EffectPrefabId) == false)
                return effect.EffectPrefabId.Trim();

            return string.IsNullOrWhiteSpace(effect.StatusEffectId)
                ? string.Empty
                : effect.StatusEffectId.Trim();
        }

        private static bool IsSpecialPassiveEffect(RelicEffectDefinition effect)
        {
            string effectId = GetSpecialEffectId(effect);
            return effectId is CautiousSlugId or RepulsionArmorPlateId or FocusCrystalId or
                CrowbarId or UnstableTeslaCoilId or LuckyRerollId or ShapedGlassDamageId or
                LumpOfCoalId or ToughLoveId or
                WhoreOfBabylonId or MoneyEqualsPowerId or WaferId or StopWatchId or
                SpikyShieldId or IronHammerId or TurboSkatesId or OverpoweredChaliceId;
        }

        private void InitializeSpecialState(RelicRuntimeState state)
        {
            if (HasEffect(state, HolyMantleId))
                state.CustomCounters[HolyMantleReadyKey] = 1f;

            if (HasEffect(state, UnstableTeslaCoilId))
                state.CustomCounters[TeslaCycleStartKey] = Time.time;
        }

        private void ResetSpecialRoomState(RelicRuntimeState state)
        {
            if (HasEffect(state, HolyMantleId))
                state.CustomCounters[HolyMantleReadyKey] = 1f;

            if (HasEffect(state, CancerId))
                state.CustomCounters[CancerActiveKey] = 0f;
        }

        private void RemoveSpecialStateModifiers(RelicRuntimeState state)
        {
            _statModifierLayer.RemoveModifiers(GetWhoreModifierSource(state));
            _statModifierLayer.RemoveModifiers(GetSpecialModifierSource(state, SpikyShieldId));
            _statModifierLayer.RemoveModifiers(GetSpecialModifierSource(state, TurboSkatesId));

            string[] temporarySources = _temporaryModifierOwners
                .Where(pair => pair.Value == state)
                .Select(pair => pair.Key)
                .ToArray();

            foreach (string sourceId in temporarySources)
            {
                _statModifierLayer.RemoveModifiers(sourceId);
                _temporaryModifierOwners.Remove(sourceId);
            }
        }

        private void DisposeSpecialRelics()
        {
            foreach (RelicRuntimeState state in _activeRelics)
            {
                _statModifierLayer.RemoveModifiers(GetSpecialModifierSource(state, SpikyShieldId));
                _statModifierLayer.RemoveModifiers(GetSpecialModifierSource(state, TurboSkatesId));
            }

            foreach (string sourceId in _temporaryModifierOwners.Keys.ToArray())
                _statModifierLayer.RemoveModifiers(sourceId);

            _temporaryModifierOwners.Clear();
            _activeVenomBladePoisons.Clear();
            ApplyStopWatchSlow(1f);
            _lastStopWatchMultiplier = 1f;
        }

        private static string GetWhoreModifierSource(RelicRuntimeState state) =>
            $"relic-special:{state.Definition.Id}:{WhoreOfBabylonId}";

        private static string GetSpecialModifierSource(RelicRuntimeState state, string effectId) =>
            $"relic-special:{state.Definition.Id}:{effectId}";
    }
}
