using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Create UpgradeOfferConfiguration", fileName = "UpgradeOfferConfiguration")]
public class UpgradeOfferConfiguration : ScriptableObject
{
    [field: SerializeField] public UpgradeOfferItemFacade UpgradeOfferItemFacade { get; private set; }
    [field: SerializeField, Min(1)] public int MaxBuildSlots { get; private set; } = 5;
    [field: SerializeField, Min(1)] public int MaxActiveAbilities { get; private set; } = 3;
    [field: SerializeField, Min(1)] public int MaxPassiveAbilities { get; private set; } = 3;
    [field: SerializeField, Range(0f, 100f)] public float ActiveAbilityOfferChance { get; private set; } = 50f;
    [field: SerializeField, Range(0f, 100f)] public float AdditionalProjectilesUpgradeOfferChance { get; private set; } = 40f;
    [field: SerializeField, Min(0.01f)] public float ProjectileCountIncreaseStep { get; private set; } = 0.25f;
    [field: SerializeField] public UpgradeRarityData[] RarityData { get; private set; } =
    {
        new(UpgradeRarity.Common, 1f, 70f, -10f, 0.25f, 0.75f),
        new(UpgradeRarity.Rare, 1.5f, 22f, 7f, 0.5f, 1.25f),
        new(UpgradeRarity.Epic, 2f, 7f, 2f, 1f, 2f),
        new(UpgradeRarity.Legendary, 3f, 1f, 1f, 1.5f, 3f)
    };
    [field: SerializeField] public UpgradeItemSpriteSet[] ItemSpriteSets { get; private set; } =
        Array.Empty<UpgradeItemSpriteSet>();

    public UpgradeRarityData GetRarityData(UpgradeRarity rarity)
    {
        UpgradeRarityData[] rarityData = GetRarityDataOrDefaults();
        return rarityData.FirstOrDefault(data => data.Rarity == rarity) ??
               rarityData.FirstOrDefault(data => data.Rarity == UpgradeRarity.Common) ??
               GetDefaultRarityData()[0];
    }

    public UpgradeRarityData GetRandomRarityData(int rejectedOfferCount = 0)
    {
        UpgradeRarityData[] rarityData = GetRarityDataOrDefaults();
        int rejectionCount = Mathf.Max(0, rejectedOfferCount);
        float totalWeight = rarityData.Sum(data => GetAdjustedRarityWeight(data, rejectionCount));

        if (totalWeight <= 0f)
            return GetRarityData(UpgradeRarity.Common);

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        foreach (UpgradeRarityData data in rarityData)
        {
            roll -= GetAdjustedRarityWeight(data, rejectionCount);
            if (roll <= 0f)
                return data;
        }

        return rarityData[^1];
    }

    public float GetRandomProjectileCountIncrease(UpgradeRarityData rarityData)
    {
        float step = Mathf.Max(0.01f, ProjectileCountIncreaseStep);
        if (rarityData == null)
            return step;

        float minIncrease = Mathf.Max(step, rarityData.MinProjectileCountIncrease);
        float maxIncrease = Mathf.Max(minIncrease, rarityData.MaxProjectileCountIncrease);
        int stepCount = Mathf.FloorToInt((maxIncrease - minIncrease) / step + 0.0001f);
        float increase = minIncrease + UnityEngine.Random.Range(0, stepCount + 1) * step;
        return Mathf.Min(maxIncrease, increase);
    }

    public UpgradeItemSpriteSet GetItemSpriteSet(UpgradeItemType itemType)
    {
        UpgradeItemSpriteSet[] itemSpriteSets = GetItemSpriteSetsOrEmpty();
        return itemSpriteSets.FirstOrDefault(data => data.Type == itemType) ??
               itemSpriteSets.FirstOrDefault(data => data.Type == UpgradeItemType.New) ??
               itemSpriteSets.FirstOrDefault(data => data.Type == UpgradeItemType.Common);
    }

    private UpgradeRarityData[] GetRarityDataOrDefaults() =>
        RarityData is { Length: > 0 }
            ? RarityData
            : GetDefaultRarityData();

    private UpgradeItemSpriteSet[] GetItemSpriteSetsOrEmpty() =>
        ItemSpriteSets ?? Array.Empty<UpgradeItemSpriteSet>();

    private static UpgradeRarityData[] GetDefaultRarityData() =>
        new[]
        {
            new UpgradeRarityData(UpgradeRarity.Common, 1f, 70f, -10f, 0.25f, 0.75f),
            new UpgradeRarityData(UpgradeRarity.Rare, 1.5f, 22f, 7f, 0.5f, 1.25f),
            new UpgradeRarityData(UpgradeRarity.Epic, 2f, 7f, 2f, 1f, 2f),
            new UpgradeRarityData(UpgradeRarity.Legendary, 3f, 1f, 1f, 1.5f, 3f)
        };

    private static float GetAdjustedRarityWeight(UpgradeRarityData data, int rejectedOfferCount) =>
        Mathf.Max(0f, data.Weight + data.RejectedOfferWeightChange * rejectedOfferCount);
}

[Serializable]
public class UpgradeRarityData
{
    public UpgradeRarityData(UpgradeRarity rarity, float upgradeMultiplier, float weight,
        float rejectedOfferWeightChange, float minProjectileCountIncrease, float maxProjectileCountIncrease)
    {
        Rarity = rarity;
        UpgradeMultiplier = upgradeMultiplier;
        Weight = weight;
        RejectedOfferWeightChange = rejectedOfferWeightChange;
        MinProjectileCountIncrease = minProjectileCountIncrease;
        MaxProjectileCountIncrease = maxProjectileCountIncrease;
    }

    [field: SerializeField] public UpgradeRarity Rarity { get; private set; } = UpgradeRarity.Common;
    [field: SerializeField, Min(0f)] public float UpgradeMultiplier { get; private set; } = 1f;
    [field: SerializeField, Min(0f)] public float Weight { get; private set; } = 1f;
    [field: SerializeField, Range(-100f, 100f)] public float RejectedOfferWeightChange { get; private set; }
    [field: SerializeField, Min(0.01f)] public float MinProjectileCountIncrease { get; private set; } = 0.25f;
    [field: SerializeField, Min(0.01f)] public float MaxProjectileCountIncrease { get; private set; } = 0.75f;
}

[Serializable]
public class UpgradeItemSpriteSet
{
    [field: SerializeField] public UpgradeItemType Type { get; private set; } = UpgradeItemType.New;
    [field: SerializeField] public Sprite ItemSprite { get; private set; }
    [field: SerializeField] public Sprite BackgroundSprite { get; private set; }
}

public enum UpgradeItemType
{
    New = 0,
    Common = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

public enum UpgradeRarity
{
    Common = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

public enum AbilityUpgradeType
{
    Default = 0,
    ProjectileSpeed = 1,
    BounceRadius = 2,
    Damage = 3,
    Targets = 4,
    AdditionalProjectiles = 5,
    FireFieldDistance = 6,
    FireFieldRadius = 7,
    EarthRockRadius = 8,
    EarthRockRotationSpeed = 9,
    EarthRockStoneCount = 10,
    Cooldown = 11,
    PunchRadius = 12,
    PunchSimultaneousAttacks = 13
}

public readonly struct AbilityUpgradeEffect
{
    public AbilityUpgradeEffect(AbilityUpgradeType type, float value)
    {
        Type = type;
        Value = value;
    }

    public AbilityUpgradeType Type { get; }
    public float Value { get; }
}

public readonly struct UpgradeOffer
{
    public UpgradeOffer(CharacterAbility ability, UpgradeRarity rarity, float upgradeMultiplier,
        AbilityUpgradeType upgradeType)
        : this(ability, rarity, new AbilityUpgradeEffect(upgradeType, upgradeMultiplier))
    {
    }

    public UpgradeOffer(CharacterAbility ability, UpgradeRarity rarity, AbilityUpgradeEffect primaryUpgrade,
        AbilityUpgradeEffect? secondaryUpgrade = null)
    {
        if (ability is CharacterActiveAbility)
        {
            if (secondaryUpgrade.HasValue == false)
                throw new ArgumentException("An active ability upgrade must contain two effects.");
            if (primaryUpgrade.Type == AbilityUpgradeType.Default ||
                secondaryUpgrade.Value.Type == AbilityUpgradeType.Default)
                throw new ArgumentException("Active ability upgrade effects cannot be Default.");
            if (primaryUpgrade.Type == secondaryUpgrade.Value.Type)
                throw new ArgumentException("Active ability upgrade effects must be different.");
        }

        Ability = ability;
        HasRarity = true;
        Rarity = rarity;
        PrimaryUpgrade = primaryUpgrade;
        SecondaryUpgrade = secondaryUpgrade;
    }

    private UpgradeOffer(CharacterAbility ability)
    {
        Ability = ability;
        HasRarity = false;
        Rarity = UpgradeRarity.Common;
        PrimaryUpgrade = new AbilityUpgradeEffect(AbilityUpgradeType.Default, 1f);
        SecondaryUpgrade = null;
    }

    public CharacterAbility Ability { get; }
    public bool HasRarity { get; }
    public UpgradeRarity Rarity { get; }
    public AbilityUpgradeEffect PrimaryUpgrade { get; }
    public AbilityUpgradeEffect? SecondaryUpgrade { get; }
    public float UpgradeMultiplier => PrimaryUpgrade.Value;
    public AbilityUpgradeType UpgradeType => PrimaryUpgrade.Type;

    public UpgradeItemType ItemType =>
        HasRarity
            ? Rarity switch
            {
                UpgradeRarity.Rare => UpgradeItemType.Rare,
                UpgradeRarity.Epic => UpgradeItemType.Epic,
                UpgradeRarity.Legendary => UpgradeItemType.Legendary,
                _ => UpgradeItemType.Common
            }
            : UpgradeItemType.New;

    public static UpgradeOffer CreateNew(CharacterAbility ability) =>
        new(ability);
}
