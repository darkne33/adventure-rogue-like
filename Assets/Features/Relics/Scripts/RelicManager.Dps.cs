using System;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public sealed partial class RelicManager
    {
        // A deterministic estimate for sustained weapon hits against one boss.
        // Position/health thresholds, on-kill bonuses and separate triggered damage are excluded.
        public float GetEstimatedBossDamageMultiplier()
        {
            float bossMultiplier = 1f;
            float additiveMultiplier = 1f;
            float multiplicativeMultiplier = 1f;

            foreach (RelicRuntimeState state in _activeRelics)
            {
                if (state.IsBroken)
                    continue;

                foreach (RelicEffectDefinition effect in state.Definition.Effects ??
                                                         Array.Empty<RelicEffectDefinition>())
                {
                    if (effect.TriggerType == RelicTriggerType.PassiveStat &&
                        effect.StatType == RelicStatType.DamageMultiplier &&
                        effect.EffectPrefabId == EliteBossDamageEffectId)
                        bossMultiplier += Mathf.Max(0f, effect.Value) * state.StackCount;

                    switch (GetSpecialEffectId(effect))
                    {
                        case ToughLoveId:
                            float chance = Mathf.Clamp01(effect.Chance * state.StackCount +
                                Mathf.Max(0f, _characterStats.Luck) * 0.01f);
                            multiplicativeMultiplier *= 1f + GetEstimatedProcChance(chance) *
                                Mathf.Max(0f, effect.Value);
                            break;
                        case IronHammerId:
                            float procCount = GetEstimatedProcChance(GetEffectChance(state, effect)) *
                                              (1 + GetOverpoweredChaliceStackCount());
                            float bonkMultiplier = Mathf.Max(1f, effect.Value +
                                Mathf.Max(0, state.StackCount - 1) * Mathf.Max(0f, effect.Duration));
                            multiplicativeMultiplier *= 1f + procCount * (bonkMultiplier - 1f);
                            break;
                        case ShapedGlassDamageId:
                            multiplicativeMultiplier *= Mathf.Pow(1f + Mathf.Max(0f, effect.Value),
                                state.StackCount);
                            break;
                        default:
                            // Includes Money Equals Power; travel-dependent bonuses use no assumed distance.
                            additiveMultiplier += _buildRuntime.Modifiers.GetDamageBonus(state, effect, -1f);
                            break;
                    }
                }
            }

            // Damage/speed stat modifiers (including Soy Milk and Polyphemus) are already in weapon DPS.
            return bossMultiplier * additiveMultiplier * multiplicativeMultiplier;
        }

        private float GetEstimatedProcChance(float chance)
        {
            chance = Mathf.Clamp01(chance);
            return HasActiveEffect(LuckyRerollId) ? 1f - (1f - chance) * (1f - chance) : chance;
        }
    }
}
