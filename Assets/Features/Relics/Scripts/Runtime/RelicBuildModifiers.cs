using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Features.Relics.Scripts
{
    internal sealed class RelicBuildModifiers
    {
        private readonly IReadOnlyList<RelicRuntimeState> _relics;
        private readonly CharacterStats _stats;
        private readonly CharacterWallet _wallet;

        public RelicBuildModifiers(IReadOnlyList<RelicRuntimeState> relics, CharacterStats stats,
            CharacterWallet wallet)
        {
            _relics = relics;
            _stats = stats;
            _wallet = wallet;
        }

        public bool HasPiercingProjectiles =>
            _relics.Any(state => RelicBuildEffects.FindActive(state, RelicBuildEffects.CupidsArrow) != null);

        public int ProjectileRicochetCount
        {
            get
            {
                int bounces = 0;
                foreach (RelicRuntimeState state in _relics)
                {
                    RelicEffectDefinition effect = RelicBuildEffects.FindActive(state, RelicBuildEffects.RubberCement);
                    if (effect != null)
                        bounces += Mathf.Max(1, Mathf.RoundToInt(effect.Value)) * state.StackCount;
                }
                return Mathf.Clamp(bounces, 0, 12);
            }
        }

        public void Refresh()
        {
            float damage = 1f;
            float attackSpeed = 1f;
            foreach (RelicRuntimeState state in _relics)
            {
                if (state.IsBroken)
                    continue;
                foreach (RelicEffectDefinition effect in state.Definition.Effects ?? Array.Empty<RelicEffectDefinition>())
                {
                    if (RelicBuildEffects.GetId(effect) is not (RelicBuildEffects.SoyMilk or RelicBuildEffects.Polyphemus))
                        continue;
                    damage *= Mathf.Pow(Mathf.Max(0.01f, effect.Value), state.StackCount);
                    attackSpeed *= Mathf.Pow(Mathf.Max(0.01f, effect.BossValue), state.StackCount);
                }
            }
            _stats.RelicDamageMultiplier = damage;
            _stats.RelicAttackSpeedMultiplier = attackSpeed;
        }

        // Called inside the existing damage loop to preserve additive ordering and proc rolls.
        public float GetDamageBonus(RelicRuntimeState state, RelicEffectDefinition effect, float travelDistance)
        {
            if (state.IsBroken)
                return 0f;
            return RelicBuildEffects.GetId(effect) switch
            {
                RelicBuildEffects.LumpOfCoal when travelDistance >= 0f =>
                    Mathf.Min(Mathf.Max(0f, effect.Cap), travelDistance * Mathf.Max(0f, effect.Value) * state.StackCount),
                RelicBuildEffects.MoneyEqualsPower =>
                    Mathf.Min(Mathf.Max(0f, effect.Cap), Mathf.Max(0, _wallet.Gold.Count) *
                        Mathf.Max(0f, effect.Value) * state.StackCount),
                _ => 0f
            };
        }

        public void Reset()
        {
            _stats.RelicDamageMultiplier = 1f;
            _stats.RelicAttackSpeedMultiplier = 1f;
        }
    }
}
