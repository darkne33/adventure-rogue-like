using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Character/CharacterSettingsConfiguration",
    fileName = "CharacterSettingsConfiguration", order = 0)]
public class CharacterSettingsConfiguration : ScriptableObject
{
    [Header("Damage and fight")]
    [field: SerializeField] public float Damage { get; private set; }
    [field: SerializeField] public float AttackSpeed { get; private set; }
    [field: SerializeField] public float AbilityDuration { get; private set; }
    [field: SerializeField] public float CritChance { get; private set; }
    [field: SerializeField] public float CritDamage { get; private set; }
    [field: SerializeField] public float LifeSteal { get; private set; }
    [field: SerializeField] public float ThornsDamage { get; private set; }

    [Header("Survival")]
    [field: SerializeField] public int MaxHp { get; private set; } = 54;
    [field: SerializeField] public float RegenHp { get; private set; }
    [field: SerializeField] public float Armor { get; private set; }
    [field: SerializeField] public float Evasion { get; private set; }

    [Header("Economic and progress")]
    [field: SerializeField] public float GainHp { get; private set; }
    [field: SerializeField] public float Luck { get; private set; }
    [field: SerializeField] public float GainGold { get; private set; }

    [field: Header("Movement Settings")] 
    [field: SerializeField] public float MovementSpeed { get; private set; } = 10;

    [field: SerializeField] public float RotationSpeed { get; private set; }
    [field: SerializeField] public float Acceleration { get; private set; }
    [field: SerializeField] public float Deceleration { get; private set; }
    [field: SerializeField] public float JumpForce { get; private set; }
    [field: SerializeField] public float GravityMultiplier { get; private set; }
}