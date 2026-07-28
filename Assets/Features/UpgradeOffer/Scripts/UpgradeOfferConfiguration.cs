using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Create UpgradeOfferConfiguration", fileName = "UpgradeOfferConfiguration")]
public class UpgradeOfferConfiguration : ScriptableObject
{
    [field: SerializeField] public UpgradeOfferItemFacade UpgradeOfferItemFacade { get; private set; }
    [field: SerializeField, Min(1)] public int MaxBuildSlots { get; private set; } = 5;
    [field: SerializeField, Min(1)] public int MaxActiveAbilities { get; private set; } = 3;
    [field: SerializeField, Range(0f, 100f)] public float SecondActiveAbilityOfferChance { get; private set; } = 100f;
    [field: SerializeField, Range(0f, 100f)] public float ThirdActiveAbilityOfferChance { get; private set; } = 10f;
    [field: SerializeField, Range(0f, 100f)] public float ActiveAbilityOfferChance { get; private set; } = 50f;
    [field: SerializeField, Min(0.01f)] public float ProjectileCountIncreaseStep { get; private set; } = 0.25f;
    [field: SerializeField] public UpgradeRarityData[] RarityData { get; private set; } =
    {
        new(UpgradeRarity.Common, 1f, 70f, 0.25f, 0.75f),
        new(UpgradeRarity.Rare, 1.5f, 22f, 0.5f, 1.25f),
        new(UpgradeRarity.Epic, 2f, 7f, 1f, 2f),
        new(UpgradeRarity.Legendary, 3f, 1f, 1.5f, 3f)
    };
    [field: SerializeField] public UpgradeItemSpriteSet[] ItemSpriteSets { get; private set; } =
        Array.Empty<UpgradeItemSpriteSet>();

    public float GetNewActiveAbilityOfferChance(int activeAbilityCount)
    {
        if (activeAbilityCount >= Mathf.Max(1, MaxActiveAbilities))
            return 0f;

        return activeAbilityCount <= 1
            ? SecondActiveAbilityOfferChance
            : ThirdActiveAbilityOfferChance;
    }

    public UpgradeRarityData GetRarityData(UpgradeRarity rarity)
    {
        UpgradeRarityData[] rarityData = GetRarityDataOrDefaults();
        return rarityData.FirstOrDefault(data => data.Rarity == rarity) ??
               rarityData.FirstOrDefault(data => data.Rarity == UpgradeRarity.Common) ??
               GetDefaultRarityData()[0];
    }

    public UpgradeRarityData GetRandomRarityData()
    {
        UpgradeRarityData[] rarityData = GetRarityDataOrDefaults();
        float totalWeight = rarityData.Sum(data => Mathf.Max(0f, data.Weight));

        if (totalWeight <= 0f)
            return GetRarityData(UpgradeRarity.Common);

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        foreach (UpgradeRarityData data in rarityData)
        {
            roll -= Mathf.Max(0f, data.Weight);
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
            new UpgradeRarityData(UpgradeRarity.Common, 1f, 70f, 0.25f, 0.75f),
            new UpgradeRarityData(UpgradeRarity.Rare, 1.5f, 22f, 0.5f, 1.25f),
            new UpgradeRarityData(UpgradeRarity.Epic, 2f, 7f, 1f, 2f),
            new UpgradeRarityData(UpgradeRarity.Legendary, 3f, 1f, 1.5f, 3f)
        };
}

[Serializable]
public class UpgradeRarityData
{
    public UpgradeRarityData(UpgradeRarity rarity, float upgradeMultiplier, float weight,
        float minProjectileCountIncrease, float maxProjectileCountIncrease)
    {
        Rarity = rarity;
        UpgradeMultiplier = upgradeMultiplier;
        Weight = weight;
        MinProjectileCountIncrease = minProjectileCountIncrease;
        MaxProjectileCountIncrease = maxProjectileCountIncrease;
    }

    [field: SerializeField] public UpgradeRarity Rarity { get; private set; } = UpgradeRarity.Common;
    [field: SerializeField, Min(0f)] public float UpgradeMultiplier { get; private set; } = 1f;
    [field: SerializeField, Min(0f)] public float Weight { get; private set; } = 1f;
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
    ProjectileSpeedCooldown = 1,
    BounceRadiusDamage = 2,
    Damage = 3,
    TargetsDamage = 4,
    AdditionalProjectiles = 5
}

public readonly struct UpgradeOffer
{
    public UpgradeOffer(CharacterAbility ability, UpgradeRarity rarity, float upgradeMultiplier,
        AbilityUpgradeType upgradeType)
    {
        Ability = ability;
        HasRarity = true;
        Rarity = rarity;
        UpgradeMultiplier = upgradeMultiplier;
        UpgradeType = upgradeType;
    }

    private UpgradeOffer(CharacterAbility ability)
    {
        Ability = ability;
        HasRarity = false;
        Rarity = UpgradeRarity.Common;
        UpgradeMultiplier = 1f;
        UpgradeType = AbilityUpgradeType.Default;
    }

    public CharacterAbility Ability { get; }
    public bool HasRarity { get; }
    public UpgradeRarity Rarity { get; }
    public float UpgradeMultiplier { get; }
    public AbilityUpgradeType UpgradeType { get; }

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
