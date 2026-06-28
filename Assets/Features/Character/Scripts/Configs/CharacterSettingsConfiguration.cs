using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Character/CharacterSettingsConfiguration",
    fileName = "CharacterSettingsConfiguration", order = 0)]
public class CharacterSettingsConfiguration : ScriptableObject
{
    [field: Header("Damage and fight")]
    [field: SerializeField] public float DamageInPercent { get; private set; }
    [field: SerializeField] public float AttackSpeed { get; private set; }
    [field: SerializeField] public float AbilityDuration { get; private set; }
    [field: SerializeField] public float CritChance { get; private set; }
    [field: SerializeField] public float CritDamage { get; private set; }
    [field: SerializeField] public float LifeSteal { get; private set; }
    [field: SerializeField] public float ThornsDamage { get; private set; }
    [field: SerializeField] public float CooldownReduction { get; private set; }
    [field: SerializeField] public float ProjectileCount { get; private set; }

    [field: Header("Survival")]
    [field: SerializeField] public int MaxHp { get; private set; } = 54;
    [field: SerializeField] public float RegenHp { get; private set; }
    [field: SerializeField] public float Armor { get; private set; }
    [field: SerializeField] public float Evasion { get; private set; }

    [field: Header("Economic and progress")]
    [field: SerializeField] public float GainHp { get; private set; }
    [field: SerializeField] public float Luck { get; private set; }
    [field: SerializeField] public float GainGold { get; private set; }
    [field: SerializeField] public float XPBonus { get; private set; }
    [field: SerializeField] public float PickupRange { get; private set; }

    [field: Header("Movement Settings")] 
    [field: SerializeField] public float MovementSpeed { get; private set; } = 10;

    [field: SerializeField] public float RotationSpeed { get; private set; }
    [field: SerializeField] public float Acceleration { get; private set; }
    [field: SerializeField] public float Deceleration { get; private set; }
    [field: SerializeField] public float JumpForce { get; private set; }
    [field: SerializeField] public float JumpForwardImpulse { get; private set; } = 1.35f;
    [field: SerializeField] public float JumpInertiaDuration { get; private set; } = 0.65f;
    [field: SerializeField, Range(0f, 1f)] public float JumpInertiaAirControl { get; private set; } = 0.18f;
    [field: SerializeField] public float GravityMultiplier { get; private set; }
    [field: SerializeField] public float CoyoteTime { get; private set; } = 0.12f;
    [field: SerializeField] public float BunnyHopResetDelay { get; private set; } = 0.25f;
    [field: SerializeField] public float BunnyHopSpeedBonusPerJump { get; private set; } = 0.045f;
    [field: SerializeField] public float MaxBunnyHopSpeedBonus { get; private set; } = 0.18f;
    [field: SerializeField, Range(-1f, 1f)] public float BunnyHopCameraAlignment { get; private set; } = 0.25f;
    [field: SerializeField] public float BunnyHopCameraTurnSlowdownSpeed { get; private set; } = 260f;
    [field: SerializeField] public float BunnyHopCameraTurnSlowdownStrength { get; private set; } = 0.35f;
    [field: SerializeField] public float DefaultAirAcceleration { get; private set; } = 22f;
    [field: SerializeField] public float AirTurnSpeed { get; private set; } = 210f;
    [field: SerializeField] public float DefaultAirDeceleration { get; private set; } = 16f;
    [field: SerializeField] public float LandingSlideDuration { get; private set; } = 0.16f;
    [field: SerializeField] public float LandingSlideDeceleration { get; private set; } = 8f;
    [field: SerializeField] public float LandingSlideSpeedMultiplier { get; private set; } = 1.04f;
    [field: SerializeField, Range(0f, 1f)] public float LandingSlideInputCarry { get; private set; } = 0.45f;
}
