using System;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public enum RelicRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Legendary = 3
    }

    internal static class RelicRarityPalette
    {
        public static Color GetColor(RelicRarity rarity) =>
            rarity switch
            {
                RelicRarity.Common => new Color(0.28f, 0.85f, 0.28f),
                RelicRarity.Uncommon => new Color(0.2f, 0.55f, 1f),
                RelicRarity.Rare => new Color(0.85f, 0.25f, 1f),
                RelicRarity.Legendary => new Color(1f, 0.75f, 0.12f),
                _ => Color.white
            };
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
        OnFatalDamage,
        OnRoomCompleted
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
}
