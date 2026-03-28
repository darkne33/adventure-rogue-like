using System;

[Serializable]
public class CharacterStats
{
    //Damage and fight
    public float Damage;
    public float AttackSpeed;
    public float AbilityDuration;
    public float CritChance;
    public float CritDamage;
    public float LifeSteal;
    public float ThornsDamage;
    
    //Survival
    public float MaxHp;
    public float RegenHp;
    public float Armor;
    public float Evasion;
    
    //Economic and progress
    public float GainHp;
    public float Luck;
    public float GainGold;
    
    //Movement
    public float MovementSpeed;
}