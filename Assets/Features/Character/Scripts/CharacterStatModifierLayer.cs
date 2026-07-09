using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterStatModifierLayer
{
    private const float PercentMultiplier = 100f;
    private const float CritChanceCap = 100f;
    private const float EvasionCap = 60f;
    private const float CooldownReductionCap = 80f;

    private readonly CharacterStats _characterStats;
    private readonly Dictionary<string, List<CharacterStatModifier>> _modifiersBySource = new();
    private readonly Dictionary<StatType, float> _appliedValuesByStat = new();

    public CharacterStatModifierLayer(CharacterStats characterStats)
    {
        _characterStats = characterStats;
    }

    public void AddModifier(string sourceId, StatType stat, float value,
        CharacterStatModifierStackingType stackingType)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Modifier source id cannot be empty.", nameof(sourceId));

        sourceId = sourceId.Trim();
        if (_modifiersBySource.TryGetValue(sourceId, out List<CharacterStatModifier> modifiers) == false)
        {
            modifiers = new List<CharacterStatModifier>();
            _modifiersBySource[sourceId] = modifiers;
        }

        modifiers.Add(new CharacterStatModifier(stat, value, stackingType));
        RecalculateStat(stat);
    }

    public void RemoveModifiers(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        sourceId = sourceId.Trim();
        if (_modifiersBySource.TryGetValue(sourceId, out List<CharacterStatModifier> modifiers) == false)
            return;

        HashSet<StatType> affectedStats = CollectStats(modifiers);
        _modifiersBySource.Remove(sourceId);

        foreach (StatType stat in affectedStats)
            RecalculateStat(stat);
    }

    public void ClearModifiers()
    {
        HashSet<StatType> affectedStats = new(_appliedValuesByStat.Keys);
        foreach (List<CharacterStatModifier> modifiers in _modifiersBySource.Values)
            affectedStats.UnionWith(CollectStats(modifiers));

        _modifiersBySource.Clear();

        foreach (StatType stat in affectedStats)
            RecalculateStat(stat);
    }

    public void Reset()
    {
        _modifiersBySource.Clear();
        _appliedValuesByStat.Clear();
    }

    private void RecalculateStat(StatType stat)
    {
        float currentValue = GetStatValue(stat);
        _appliedValuesByStat.TryGetValue(stat, out float oldAppliedValue);

        float baseValue = currentValue - oldAppliedValue;
        float newAppliedValue = CalculateAppliedValue(stat, baseValue);
        float finalValue = ClampStatValue(stat, baseValue + newAppliedValue);

        SetStatValue(stat, finalValue);

        if (Mathf.Approximately(newAppliedValue, 0f))
            _appliedValuesByStat.Remove(stat);
        else
            _appliedValuesByStat[stat] = finalValue - baseValue;
    }

    private float CalculateAppliedValue(StatType stat, float baseValue)
    {
        float flatValue = 0f;
        float additivePercentValue = 0f;
        float percentOfBaseValue = 0f;
        float multiplicativeValue = 1f;

        foreach (List<CharacterStatModifier> modifiers in _modifiersBySource.Values)
        {
            foreach (CharacterStatModifier modifier in modifiers)
            {
                if (modifier.Stat != stat)
                    continue;

                switch (modifier.StackingType)
                {
                    case CharacterStatModifierStackingType.Flat:
                        flatValue += modifier.Value;
                        break;
                    case CharacterStatModifierStackingType.AdditivePercent:
                        additivePercentValue += modifier.Value * PercentMultiplier;
                        break;
                    case CharacterStatModifierStackingType.PercentOfBase:
                        percentOfBaseValue += modifier.Value;
                        break;
                    case CharacterStatModifierStackingType.MultiplicativePercent:
                        multiplicativeValue *= 1f + modifier.Value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(modifier.StackingType), modifier.StackingType,
                            null);
                }
            }
        }

        return flatValue +
               additivePercentValue +
               baseValue * percentOfBaseValue +
               baseValue * (multiplicativeValue - 1f);
    }

    private static HashSet<StatType> CollectStats(IEnumerable<CharacterStatModifier> modifiers)
    {
        var stats = new HashSet<StatType>();
        foreach (CharacterStatModifier modifier in modifiers)
            stats.Add(modifier.Stat);

        return stats;
    }

    private float GetStatValue(StatType stat) =>
        stat switch
        {
            StatType.Damage => _characterStats.DamageInPercent,
            StatType.AttackSpeed => _characterStats.AttackSpeed,
            StatType.AbilityDuration => _characterStats.AbilityDuration,
            StatType.CritChance => _characterStats.CritChance,
            StatType.CritDamage => _characterStats.CritDamage,
            StatType.LifeSteal => _characterStats.LifeSteal,
            StatType.ThornsDamage => _characterStats.ThornsDamage,
            StatType.MaxHp => _characterStats.MaxHp,
            StatType.RegenHp => _characterStats.RegenHp,
            StatType.Shield => _characterStats.Shield,
            StatType.Armor => _characterStats.Armor,
            StatType.Evasion => _characterStats.Evasion,
            StatType.GainHp => _characterStats.GainHp,
            StatType.Luck => _characterStats.Luck,
            StatType.GainGold => _characterStats.GainGold,
            StatType.MovementSpeed => _characterStats.MovementSpeed,
            StatType.XPBonus => _characterStats.XPBonus,
            StatType.PickupRange => _characterStats.PickupRange,
            StatType.ProjectileCount => _characterStats.ProjectileCount,
            StatType.CooldownReduction => _characterStats.CooldownReduction,
            StatType.MovementAcceleration => _characterStats.MovementAcceleration,
            StatType.JumpForce => _characterStats.JumpForce,
            StatType.RotationSpeed => _characterStats.RotationSpeed,
            StatType.GravityMultiplier => _characterStats.GravityMultiplier,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };

    private void SetStatValue(StatType stat, float value)
    {
        switch (stat)
        {
            case StatType.Damage:
                _characterStats.DamageInPercent = value;
                break;
            case StatType.AttackSpeed:
                _characterStats.AttackSpeed = value;
                break;
            case StatType.AbilityDuration:
                _characterStats.AbilityDuration = value;
                break;
            case StatType.CritChance:
                _characterStats.CritChance = value;
                break;
            case StatType.CritDamage:
                _characterStats.CritDamage = value;
                break;
            case StatType.LifeSteal:
                _characterStats.LifeSteal = value;
                break;
            case StatType.ThornsDamage:
                _characterStats.ThornsDamage = value;
                break;
            case StatType.MaxHp:
                _characterStats.MaxHp = value;
                break;
            case StatType.RegenHp:
                _characterStats.RegenHp = value;
                break;
            case StatType.Shield:
                _characterStats.Shield = value;
                break;
            case StatType.Armor:
                _characterStats.Armor = value;
                break;
            case StatType.Evasion:
                _characterStats.Evasion = value;
                break;
            case StatType.GainHp:
                _characterStats.GainHp = value;
                break;
            case StatType.Luck:
                _characterStats.Luck = value;
                break;
            case StatType.GainGold:
                _characterStats.GainGold = value;
                break;
            case StatType.MovementSpeed:
                _characterStats.MovementSpeed = value;
                break;
            case StatType.XPBonus:
                _characterStats.XPBonus = value;
                break;
            case StatType.PickupRange:
                _characterStats.PickupRange = value;
                break;
            case StatType.ProjectileCount:
                _characterStats.ProjectileCount = value;
                break;
            case StatType.CooldownReduction:
                _characterStats.CooldownReduction = value;
                break;
            case StatType.MovementAcceleration:
                _characterStats.MovementAcceleration = value;
                break;
            case StatType.JumpForce:
                _characterStats.JumpForce = value;
                break;
            case StatType.RotationSpeed:
                _characterStats.RotationSpeed = value;
                break;
            case StatType.GravityMultiplier:
                _characterStats.GravityMultiplier = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
        }
    }

    private static float ClampStatValue(StatType stat, float value) =>
        stat switch
        {
            StatType.CritChance => Mathf.Clamp(value, 0f, CritChanceCap),
            StatType.Evasion => Mathf.Clamp(value, 0f, EvasionCap),
            StatType.CooldownReduction => Mathf.Clamp(value, 0f, CooldownReductionCap),
            StatType.MaxHp or StatType.RegenHp or StatType.Shield or StatType.Armor or StatType.GainHp or
                StatType.MovementSpeed or StatType.MovementAcceleration or StatType.JumpForce or
                StatType.RotationSpeed or StatType.GravityMultiplier or StatType.ProjectileCount =>
                Mathf.Max(0f, value),
            _ => value
        };

    private readonly struct CharacterStatModifier
    {
        public readonly StatType Stat;
        public readonly float Value;
        public readonly CharacterStatModifierStackingType StackingType;

        public CharacterStatModifier(StatType stat, float value,
            CharacterStatModifierStackingType stackingType)
        {
            Stat = stat;
            Value = value;
            StackingType = stackingType;
        }
    }
}
