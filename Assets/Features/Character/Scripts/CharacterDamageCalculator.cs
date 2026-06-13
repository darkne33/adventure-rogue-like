using UnityEngine;

public class CharacterDamageCalculator
{
    private const float PERCENT_MULTIPLIER = 0.01f;

    private readonly CharacterStats _characterStats;

    public CharacterDamageCalculator(CharacterStats characterStats) =>
        _characterStats = characterStats;

    public CharacterDamageResult Calculate(int baseDamage)
    {
        float damageMultiplier = 1f + Mathf.Max(0f, _characterStats.DamageInPercent) * PERCENT_MULTIPLIER;
        int modifiedDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageMultiplier));

        float critChance = Mathf.Clamp(_characterStats.CritChance, 0f, 100f);
        bool isCritical = Random.value < critChance * PERCENT_MULTIPLIER;

        if (!isCritical)
            return new CharacterDamageResult(modifiedDamage, false);

        float critMultiplier = 1f + Mathf.Max(0f, _characterStats.CritDamage) * PERCENT_MULTIPLIER;
        int criticalDamage = Mathf.Max(modifiedDamage, Mathf.RoundToInt(modifiedDamage * critMultiplier));

        return new CharacterDamageResult(criticalDamage, true);
    }
}
