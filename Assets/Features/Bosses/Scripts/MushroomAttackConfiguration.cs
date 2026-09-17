using System;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    public abstract class MushroomAttackConfiguration : BossAttackConfiguration
    {
        [field: Header("Attack timing and damage")]
        [field: SerializeField, Min(0.01f)] public float Duration { get; protected set; } = 1.875f;
        [field: SerializeField, Range(0.01f, 1f)]
        [field: Tooltip("Normalized clip position where ground contact, damage and the impact effect occur together.")]
        public float ImpactNormalized { get; protected set; } = 0.6666667f;
        [field: SerializeField, Min(0.01f)] public float Radius { get; protected set; } = 2.3f;
        [field: SerializeField, Min(0)] public int Damage { get; protected set; } = 20;
        [field: SerializeField, Min(0f)] public float RecoveryDuration { get; protected set; } = 0.2f;

        [field: Header("Fixed ground warning")]
        [field: SerializeField]
        [field: Tooltip("Unit-diameter warning placed at the captured impact position before the attack.")]
        public GameObject IndicatorPrefab { get; protected set; }
        [field: SerializeField, Range(0.01f, 1f)] public float InitialIndicatorScale { get; protected set; } = 0.05f;
        [field: SerializeField] public float IndicatorGroundOffset { get; protected set; } = 0.035f;

        [field: Header("Impact effect and knockback")]
        [field: SerializeField] public GameObject EffectPrefab { get; protected set; }
        [field: SerializeField, Min(0.01f)] public float EffectLifetime { get; protected set; } = 3f;
        [field: SerializeField, Min(0f)] public float KnockbackForce { get; protected set; } = 10f;
        [field: SerializeField, Min(0f)] public float KnockbackUpwardForce { get; protected set; } = 10f;

        public override IBossAttack CreateAttack(BossFacade boss, CharacterFacade character)
        {
            if (boss is not MushroomBossFacade mushroom)
                throw new ArgumentException("Mushroom attacks require a MushroomBossFacade.", nameof(boss));

            return new MushroomBossAttack(this, mushroom);
        }
    }
}
