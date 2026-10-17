public class CharacterScrollMaxHpAbility : CharacterPassiveAbility
{
    private readonly System.Collections.Generic.Stack<float> _appliedIncreases = new();

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
        if (_appliedIncreases.Count > 0)
            characterStats.MaxHp = CharacterHealthRules.ClampMaximum(
                characterStats.MaxHp - _appliedIncreases.Pop());
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        float previousMaximum = characterStats.MaxHp;
        characterStats.MaxHp = GetStatToIncrease(characterStats, 1f);
        _appliedIncreases.Push(characterStats.MaxHp - previousMaximum);
    }

    public override float GetStatFromIncrease(CharacterStats characterStats) => 
        characterStats.MaxHp;

    public override float GetStatToIncrease(CharacterStats characterStats, float upgradeMultiplier) => 
        CharacterHealthRules.ClampMaximum(GetStatFromIncrease(characterStats) + CharacterHealthRules.HalfHeart);
}
