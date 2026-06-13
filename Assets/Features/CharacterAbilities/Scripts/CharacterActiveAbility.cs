using UnityEngine;

public abstract class CharacterActiveAbility : CharacterAbility
{
    private const float PERCENT_MULTIPLIER = 0.01f;

    private CharacterStats _characterStats;

    public float Cooldown { get; protected set; }
    public float CurrentCooldown { get; set; }

    public float Stat_1 { get; protected set; }
    public string StatName_1 { get; protected set; }

    public float Stat_2 { get; protected set; }
    public string StatName_2 { get; protected set; }

    public virtual bool CanUse(CharacterFacade character) =>
        CurrentCooldown <= 0f && IsReady(character);

    protected virtual bool IsReady(CharacterFacade character) => true;

    public virtual void Use(CharacterFacade character)
    {
        if (!CanUse(character))
            return;

        OnUse(character);

        float attackSpeedMultiplier =
            1f + Mathf.Max(0f, _characterStats?.AttackSpeed ?? 0f) * PERCENT_MULTIPLIER;
        CurrentCooldown = Cooldown / attackSpeedMultiplier;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        _characterStats = characterStats;
    }

    protected float AbilityDurationMultiplier =>
        1f + Mathf.Max(0f, _characterStats?.AbilityDuration ?? 0f) * PERCENT_MULTIPLIER;

    protected abstract void OnUse(CharacterFacade character);

    public abstract float GetStatFromIncrease();
    public abstract float GetStatToIncrease();
}
