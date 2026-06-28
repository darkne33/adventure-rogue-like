using System;

[Serializable]
public class CharacterStats
{
    //Damage and fight
    public float DamageInPercent;
    public float AttackSpeed;
    public float AbilityDuration;
    public float CritChance;
    public float CritDamage;
    public float LifeSteal;
    public float ThornsDamage;
    public float CooldownReduction;
    public float ProjectileCount;
    
    //Survival
    public float MaxHp;
    public float RegenHp;
    public float Armor;
    public float Evasion;
    
    //Economic and progress
    public float GainHp;
    public float Luck;
    public float GainGold;
    public float XPBonus;
    public float PickupRange;
    
    //Movement
    public float MovementSpeed;
    public float MovementAcceleration;
    public float JumpForce;
    public float JumpForwardImpulse;
    public float JumpInertiaDuration;
    public float JumpInertiaAirControl;
    public float RotationSpeed;
    public float GravityMultiplier;
    public float CoyoteTime;
    public float BunnyHopResetDelay;
    public float BunnyHopSpeedBonusPerJump;
    public float MaxBunnyHopSpeedBonus;
    public float BunnyHopCameraAlignment;
    public float BunnyHopCameraTurnSlowdownSpeed;
    public float BunnyHopCameraTurnSlowdownStrength;
    public float DefaultAirAcceleration;
    public float AirTurnSpeed;
    public float DefaultAirDeceleration;
    public float LandingSlideDuration;
    public float LandingSlideDeceleration;
    public float LandingSlideSpeedMultiplier;
    public float LandingSlideInputCarry;
}
