using System;
using System.Collections.Generic;
using Features.Bosses.UI;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Boss Config", fileName = "BossConfig")]
    public sealed class BossConfig : ScriptableObject
    {
        [field: Header("Boss HUD")]
        [field: SerializeField] public string DisplayName { get; private set; } = "Boss";
        [field: SerializeField] public BossHealthCanvas HealthCanvasPrefab { get; private set; }

        [field: Header("Health and rewards")]
        [field: SerializeField, Min(1)] public int MaxHealth { get; private set; } = 500;
        [field: SerializeField, Min(0)] public int Exp { get; private set; } = 20;
        [field: SerializeField, Min(0f)] public float DeathFadeDuration { get; private set; } = 0.25f;

        [field: Header("Animation")]
        [field: SerializeField, Min(0f)]
        [field: Tooltip("Smoothing time for attack and idle playback speed, in boss time. Zero changes speed immediately.")]
        public float AnimationSpeedBlendDuration { get; private set; } = 0.2f;

        [field: Header("Attack cycle")]
        [field: SerializeField, Min(0f)] public float InitialAttackDelay { get; private set; } = 2f;
        [field: SerializeField, Min(0.01f)]
        [field: Tooltip("Pause after the entire attack, including roots sinking underground.")]
        public float AttackInterval { get; private set; } = 4f;
        [field: SerializeField]
        [field: Tooltip("Fallback pool when no health phase matches. Enabled attacks run in order.")]
        public BossAttackConfiguration[] Attacks { get; private set; } = Array.Empty<BossAttackConfiguration>();

        [field: Header("Health phases")]
        [field: SerializeField]
        [field: Tooltip("The matching phase with the lowest health threshold supplies the attack pool. " +
                        "Use thresholds 100, 70 and 40 for three phases. Array order does not matter.")]
        public BossAttackPhase[] AttackPhases { get; private set; } = Array.Empty<BossAttackPhase>();

        public BossAttackConfiguration[] GetAttacksForHealth(float healthPercentage)
        {
            healthPercentage = Mathf.Clamp(healthPercentage, 0f, 100f);
            BossAttackPhase selectedPhase = null;
            float selectedThreshold = float.PositiveInfinity;
            if (AttackPhases != null)
            {
                foreach (BossAttackPhase phase in AttackPhases)
                {
                    if (phase == null)
                        continue;
                    float threshold = Mathf.Clamp(phase.MaxHealthPercentage, 0f, 100f);
                    if (healthPercentage <= threshold && threshold < selectedThreshold)
                    {
                        selectedPhase = phase;
                        selectedThreshold = threshold;
                    }
                }
            }

            return selectedPhase != null ? selectedPhase.Attacks : Attacks;
        }

        public IEnumerable<BossAttackConfiguration> GetAllAttacks()
        {
            if (Attacks != null)
                foreach (BossAttackConfiguration attack in Attacks)
                    yield return attack;
            if (AttackPhases == null)
                yield break;
            foreach (BossAttackPhase phase in AttackPhases)
            {
                if (phase?.Attacks == null)
                    continue;
                foreach (BossAttackConfiguration attack in phase.Attacks)
                    yield return attack;
            }
        }
    }

    [Serializable]
    public sealed class BossAttackPhase
    {
        [field: SerializeField, Range(0f, 100f)]
        [field: Tooltip("Active at or below this health percentage until a lower threshold is reached.")]
        public float MaxHealthPercentage { get; private set; } = 100f;

        [field: SerializeField]
        [field: Tooltip("The complete pool for this phase. Enabled attacks run in order, starting from the first " +
                        "attack on phase entry. An empty pool pauses attacks.")]
        public BossAttackConfiguration[] Attacks { get; private set; } = Array.Empty<BossAttackConfiguration>();
    }
}
