using System.Threading;
using Core;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;

public class RogueLikePrepareStatsState : State
{
    private readonly CharacterStats _characterStats;
    private readonly CharacterStatModifierLayer _statModifierLayer;
    private readonly CharacterSettingsConfiguration _characterSettingsConfiguration;

    public RogueLikePrepareStatsState(CharacterStats characterStats,
        CharacterStatModifierLayer statModifierLayer,
        CharacterSettingsConfiguration characterSettingsConfiguration)
    {
        _characterStats = characterStats;
        _statModifierLayer = statModifierLayer;
        _characterSettingsConfiguration = characterSettingsConfiguration;
    }

    public override async UniTask Enter(CancellationToken cts)
    {
        CharacterStatsInitialize();
        await StateMachine.EnterState<RogueLikePrepareState>();
    }

    private void CharacterStatsInitialize()
    {
        _characterStats.DamageInPercent = _characterSettingsConfiguration.DamageInPercent;
        _characterStats.AttackSpeed = _characterSettingsConfiguration.AttackSpeed;
        _characterStats.AbilityDuration = _characterSettingsConfiguration.AbilityDuration;
        _characterStats.CritChance = _characterSettingsConfiguration.CritChance;
        _characterStats.CritDamage = _characterSettingsConfiguration.CritDamage;
        _characterStats.LifeSteal = _characterSettingsConfiguration.LifeSteal;
        _characterStats.ThornsDamage = _characterSettingsConfiguration.ThornsDamage;
        _characterStats.CooldownReduction = _characterSettingsConfiguration.CooldownReduction;
        _characterStats.ProjectileCount = _characterSettingsConfiguration.ProjectileCount;
        
        _characterStats.MaxHp = _characterSettingsConfiguration.MaxHp;
        _characterStats.RegenHp = _characterSettingsConfiguration.RegenHp;
        _characterStats.Shield = _characterSettingsConfiguration.Shield;
        _characterStats.Armor = _characterSettingsConfiguration.Armor;
        _characterStats.Evasion = _characterSettingsConfiguration.Evasion;
        
        _characterStats.GainHp = _characterSettingsConfiguration.GainHp;
        _characterStats.Luck = _characterSettingsConfiguration.Luck;
        _characterStats.GainGold = _characterSettingsConfiguration.GainGold;
        _characterStats.XPBonus = _characterSettingsConfiguration.XPBonus;
        _characterStats.PickupRange = _characterSettingsConfiguration.PickupRange;
        
        _characterStats.MovementSpeed = _characterSettingsConfiguration.MovementSpeed;
        _characterStats.MovementAcceleration = _characterSettingsConfiguration.Acceleration;
        _characterStats.JumpForce = _characterSettingsConfiguration.JumpForce;
        _characterStats.JumpForwardImpulse = _characterSettingsConfiguration.JumpForwardImpulse;
        _characterStats.JumpInertiaDuration = _characterSettingsConfiguration.JumpInertiaDuration;
        _characterStats.JumpInertiaAirControl = _characterSettingsConfiguration.JumpInertiaAirControl;
        _characterStats.RotationSpeed = _characterSettingsConfiguration.RotationSpeed;
        _characterStats.GravityMultiplier = _characterSettingsConfiguration.GravityMultiplier;
        _characterStats.CoyoteTime = _characterSettingsConfiguration.CoyoteTime;
        _characterStats.BunnyHopResetDelay = _characterSettingsConfiguration.BunnyHopResetDelay;
        _characterStats.BunnyHopSpeedBonusPerJump = _characterSettingsConfiguration.BunnyHopSpeedBonusPerJump;
        _characterStats.MaxBunnyHopSpeedBonus = _characterSettingsConfiguration.MaxBunnyHopSpeedBonus;
        _characterStats.BunnyHopCameraAlignment = _characterSettingsConfiguration.BunnyHopCameraAlignment;
        _characterStats.BunnyHopCameraTurnSlowdownSpeed =
            _characterSettingsConfiguration.BunnyHopCameraTurnSlowdownSpeed;
        _characterStats.BunnyHopCameraTurnSlowdownStrength =
            _characterSettingsConfiguration.BunnyHopCameraTurnSlowdownStrength;
        _characterStats.DefaultAirAcceleration = _characterSettingsConfiguration.DefaultAirAcceleration;
        _characterStats.AirTurnSpeed = _characterSettingsConfiguration.AirTurnSpeed;
        _characterStats.DefaultAirDeceleration = _characterSettingsConfiguration.DefaultAirDeceleration;
        _characterStats.LandingSlideDuration = _characterSettingsConfiguration.LandingSlideDuration;
        _characterStats.LandingSlideDeceleration = _characterSettingsConfiguration.LandingSlideDeceleration;
        _characterStats.LandingSlideSpeedMultiplier = _characterSettingsConfiguration.LandingSlideSpeedMultiplier;
        _characterStats.LandingSlideInputCarry = _characterSettingsConfiguration.LandingSlideInputCarry;

        _statModifierLayer.Reset();
    }
}
