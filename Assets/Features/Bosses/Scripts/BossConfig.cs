using System;
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

        [field: Header("Attack cycle")]
        [field: SerializeField, Min(0f)] public float InitialAttackDelay { get; private set; } = 2f;
        [field: SerializeField, Min(0.01f)]
        [field: Tooltip("Pause after the entire attack, including roots sinking underground.")]
        public float AttackInterval { get; private set; } = 4f;
        [field: SerializeField]
        [field: Tooltip("Enabled attacks run in order. Each attack asset contains its own damage and timing.")]
        public BossAttackConfiguration[] Attacks { get; private set; } = Array.Empty<BossAttackConfiguration>();
    }
}
