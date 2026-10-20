using UnityEngine;
using UnityEngine.Serialization;

namespace Features.Relics.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Relics/Relic Chest")]
    public sealed class RelicChestConfiguration : ScriptableObject
    {
        [field: SerializeField] public GameObject ChestPrefab { get; private set; }
        [field: SerializeField] public GameObject GrayChestPrefab { get; private set; }
        [field: Header("Chest Composition")]
        [field: SerializeField, Min(0f)] public float OneGrayChestWeight { get; private set; } = 40f;
        [field: SerializeField, Min(0f)] public float TwoGrayChestsWeight { get; private set; } = 30f;
        [field: SerializeField, Min(0f)] public float OneGoldChestWeight { get; private set; } = 15f;
        [field: SerializeField, Min(0f)] public float TwoGoldChestsWeight { get; private set; } = 5f;
        [field: SerializeField, Min(0f)] public float GoldAndGrayChestWeight { get; private set; } = 10f;
        [field: Header("Free Treasure Room")]
        [field: SerializeField, Min(0f)] public float FreeTreasureOneGrayWeight { get; private set; } = 70f;
        [field: SerializeField, Min(0f)] public float FreeTreasureTwoGrayWeight { get; private set; } = 30f;
        [field: Header("Keyed Treasure Room")]
        [field: SerializeField, Min(0f)] public float KeyedTreasureOneGoldWeight { get; private set; } = 60f;
        [field: SerializeField, Min(0f)] public float KeyedTreasureGoldAndGrayWeight { get; private set; } = 25f;
        [field: SerializeField, Min(0f)] public float KeyedTreasureTwoGoldWeight { get; private set; } = 15f;
        [field: SerializeField] public GameObject RelicPickupPrefab { get; private set; }
        [field: SerializeField] public GameObject OnlyRelicPickupPrefab { get; private set; }
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
        [field: SerializeField, Min(0.01f)] public float RelicPreviewScale { get; private set; } = 0.9f;
        [field: SerializeField, Min(0f)] public float RelicPreviewRiseSpeed { get; private set; } = 0.05f;
        [field: SerializeField] public float RelicPickupDistance { get; private set; } = 3f;

        [field: Header("Gray Chest Rewards")]
        [field: FormerlySerializedAs("<GrayChestSilverRewardPrefab>k__BackingField")]
        [field: SerializeField] public GameObject GrayChestGoldRewardPrefab { get; private set; }
        [field: SerializeField] public GameObject GrayChestKeyRewardPrefab { get; private set; }
        [field: SerializeField, Min(1)] public int GrayChestMinCoins { get; private set; } = 1;
        [field: SerializeField, Min(1)] public int GrayChestMaxCoins { get; private set; } = 3;
        [field: SerializeField, Min(0f)] public float GrayChestCoinsWeight { get; private set; } = 70f;
        [field: SerializeField, Min(0f)] public float GrayChestKeyWeight { get; private set; } = 10f;
        [field: SerializeField, Min(0f)] public float GrayChestHeartWeight { get; private set; } = 10f;
        [field: SerializeField, Min(0f)] public float GrayChestCoinsAndKeyWeight { get; private set; } = 2.5f;
        [field: SerializeField, Min(0f)] public float GrayChestCoinsAndHeartWeight { get; private set; } = 5f;
        [field: SerializeField, Min(0f)] public float GrayChestHeartAndKeyWeight { get; private set; } = 2.5f;
        [field: SerializeField, Min(0.01f)] public float GrayChestRewardScale { get; private set; } = 2f;
        [field: SerializeField, Min(0f)] public float GrayChestRewardScatterRadius { get; private set; } = 1.2f;

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
