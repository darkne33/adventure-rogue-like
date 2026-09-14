using System;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Mixed Attack", fileName = "BossMixedAttackConfiguration")]
    public sealed class BossMixedAttackConfiguration : BossAttackConfiguration
    {
        [field: SerializeField]
        [field: Tooltip("Attacks run together with individual start delays. The earliest enabled attack controls " +
                        "the initial boss animation; ties use list order. Each horizontal attack takes over " +
                        "the animation when it starts. The boss waits for every attack to finish.")]
        public BossMixedAttackEntry[] Attacks { get; private set; } = Array.Empty<BossMixedAttackEntry>();

        public override IBossAttack CreateAttack(BossFacade boss, CharacterFacade character) =>
            new BossMixedAttack(this, boss, character);
    }

    [Serializable]
    public sealed class BossMixedAttackEntry
    {
        [field: SerializeField] public bool IsEnabled { get; private set; } = true;

        [field: SerializeField]
        [field: Tooltip("Any boss attack, including another mixture. Circular references are not allowed.")]
        public BossAttackConfiguration Attack { get; private set; }

        [field: SerializeField, Min(0f)]
        [field: Tooltip("Delay from the start of this mixture, before the attack's own delays and warnings. " +
                        "Zero starts immediately. Respects boss slow and stun effects.")]
        public float StartDelay { get; private set; }
    }
}
