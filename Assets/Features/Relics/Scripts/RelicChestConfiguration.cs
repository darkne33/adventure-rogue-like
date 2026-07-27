using UnityEngine;
using UnityEngine.Serialization;

namespace Features.Relics.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Relics/Relic Chest")]
    public sealed class RelicChestConfiguration : ScriptableObject
    {
        [field: SerializeField] public GameObject ChestPrefab { get; private set; }
        [field: SerializeField] public GameObject RelicPickupPrefab { get; private set; }
        [field: SerializeField, Min(0)] public int MinChestsPerLevel { get; private set; } = 1;
        [field: FormerlySerializedAs("<ChestsPerLevel>k__BackingField")]
        [field: SerializeField, Min(0)] public int MaxChestsPerLevel { get; private set; } = 2;
        [field: SerializeField, Min(0f)] public float ChestRoomOffsetRadius { get; private set; } = 4f;
        [field: SerializeField, Min(1)] public int ChestSpawnAttempts { get; private set; } = 24;
        [field: SerializeField, Min(0f)] public float GroundRayStartHeight { get; private set; } = 50f;
        [field: SerializeField, Min(0f)] public float GroundRayDistance { get; private set; } = 100f;
        [field: SerializeField] public float ChestSpawnHeight { get; private set; } = 0.45f;
        [field: SerializeField, Min(0f)] public float ObstacleCheckRadius { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float ObstacleCheckHeight { get; private set; } = 0.6f;
        [field: SerializeField] public float InteractDistance { get; private set; } = 4f;
        [field: FormerlySerializedAs("<RollDuration>k__BackingField")]
        [field: SerializeField, Min(0.1f)] public float RarityStageDuration { get; private set; } = 0.85f;
        [field: SerializeField, Min(0.02f)] public float PreviewStartInterval { get; private set; } = 0.07f;
        [field: SerializeField, Min(0.02f)] public float PreviewEndInterval { get; private set; } = 0.17f;
        [field: SerializeField, Min(0f)] public float RarityUpgradeTransitionDuration { get; private set; } = 0.3f;
        [field: SerializeField, Range(0f, 1f)] public float CommonToUncommonChance { get; private set; } = 0.35f;
        [field: SerializeField, Range(0f, 1f)] public float UncommonToRareChance { get; private set; } = 0.3f;
        [field: SerializeField, Range(0f, 1f)] public float RareToLegendaryChance { get; private set; } = 0.1f;
        [field: SerializeField, Min(0.01f)] public float RarityUpgradePumpDuration { get; private set; } = 0.32f;
        [field: SerializeField, Min(0f)] public float RarityUpgradePumpStrength { get; private set; } = 0.18f;
        [field: SerializeField, Min(0f)] public float FinalRevealDuration { get; private set; } = 0.65f;
        [field: SerializeField, Min(0f)] public float ChestShakePositionStrength { get; private set; } = 0.12f;
        [field: SerializeField, Min(0f)] public float ChestShakeRotationStrength { get; private set; } = 4f;
        [field: SerializeField, Min(1)] public int ChestShakeVibrato { get; private set; } = 28;
        [field: SerializeField, Min(0.01f)] public float RelicPreviewScale { get; private set; } = 0.9f;
        [field: SerializeField] public float RelicPickupDistance { get; private set; } = 3f;

        public float GetRarityUpgradeChance(RelicRarity rarity) =>
            rarity switch
            {
                RelicRarity.Common => CommonToUncommonChance,
                RelicRarity.Uncommon => UncommonToRareChance,
                RelicRarity.Rare => RareToLegendaryChance,
                _ => 0f
            };
    }
}
