using UnityEngine;

public class CharacterDamageCalculator
{
    private const float PERCENT_MULTIPLIER = 0.01f;

    private readonly CharacterStats _characterStats;

    public CharacterDamageCalculator(CharacterStats characterStats) =>
        _characterStats = characterStats;

    public CharacterDamageResult Calculate(int baseDamage)
    {
        int modifiedDamage = GetModifiedDamage(baseDamage);

        float critChance = Mathf.Clamp(_characterStats.CritChance, 0f, 100f);
        bool isCritical = Random.value < critChance * PERCENT_MULTIPLIER;

        if (!isCritical)
            return new CharacterDamageResult(modifiedDamage, false);

        return new CharacterDamageResult(GetCriticalDamage(modifiedDamage), true);
    }

    // Uses the mean base hit instead of rolling damage variation or consuming combat RNG.
    public float CalculateAverageDamage(int baseDamage)
    {
        if (baseDamage <= 0)
            return 0f;

        int modifiedDamage = GetModifiedDamage(baseDamage);
        float critChance = Mathf.Clamp(_characterStats.CritChance, 0f, 100f) * PERCENT_MULTIPLIER;
        return Mathf.Lerp(modifiedDamage, GetCriticalDamage(modifiedDamage), critChance);
    }

    private int GetModifiedDamage(int baseDamage)
    {
        float damageMultiplier = 1f + Mathf.Max(-90f, _characterStats.DamageInPercent) * PERCENT_MULTIPLIER;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageMultiplier *
            Mathf.Max(0.01f, _characterStats.RelicDamageMultiplier)));
    }

    private int GetCriticalDamage(int modifiedDamage)
    {
        float critMultiplier = 1f + Mathf.Max(0f, _characterStats.CritDamage) * PERCENT_MULTIPLIER;
        return Mathf.Max(modifiedDamage, Mathf.RoundToInt(modifiedDamage * critMultiplier));
    }
}
