using System.Threading;
using Core;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Features.Leaderboard;

public class RogueLikePrepareStatsState : State
{
    private readonly CharacterStats _characterStats;
    private readonly CharacterStatModifierLayer _statModifierLayer;
    private readonly CharacterConfiguration _characterConfiguration;
    private readonly RoomLeaderboardReporter _leaderboardReporter;

    public RogueLikePrepareStatsState(CharacterStats characterStats,
        CharacterStatModifierLayer statModifierLayer,
        CharacterConfiguration characterConfiguration,
        RoomLeaderboardReporter leaderboardReporter)
    {
        _characterStats = characterStats;
        _statModifierLayer = statModifierLayer;
        _characterConfiguration = characterConfiguration;
        _leaderboardReporter = leaderboardReporter;
    }

    public override async UniTask Enter(CancellationToken cts)
    {
        CharacterStatsInitialize();
        _leaderboardReporter.BeginRun();
        await StateMachine.EnterState<RogueLikePrepareState>();
    }

    private void CharacterStatsInitialize()
    {
        CharacterSettingsConfiguration settings = _characterConfiguration.CharacterSettings;

        _characterStats.DamageInPercent = settings.DamageInPercent;
        _characterStats.AttackSpeed = settings.AttackSpeed;
        _characterStats.AbilityDuration = settings.AbilityDuration;
        _characterStats.CritChance = settings.CritChance;
        _characterStats.CritDamage = settings.CritDamage;
        _characterStats.LifeSteal = settings.LifeSteal;
        _characterStats.ThornsDamage = settings.ThornsDamage;
        _characterStats.CooldownReduction = settings.CooldownReduction;
        _characterStats.ProjectileCount = settings.ProjectileCount;
        
        _characterStats.MaxHp = settings.MaxHp;
        _characterStats.RegenHp = settings.RegenHp;
        _characterStats.Shield = settings.Shield;
        _characterStats.Armor = settings.Armor;
        _characterStats.Evasion = settings.Evasion;
        
        _characterStats.GainHp = settings.GainHp;
        _characterStats.Luck = settings.Luck;
        _characterStats.GainGold = settings.GainGold;
        _characterStats.XPBonus = settings.XPBonus;
        _characterStats.PickupRange = settings.PickupRange;
        
        _characterStats.MovementSpeed = settings.MovementSpeed;
        _characterStats.MovementAcceleration = settings.Acceleration;
        _characterStats.JumpForce = settings.JumpForce;
        _characterStats.JumpForwardImpulse = settings.JumpForwardImpulse;
        _characterStats.JumpInertiaDuration = settings.JumpInertiaDuration;
        _characterStats.JumpInertiaAirControl = settings.JumpInertiaAirControl;
        _characterStats.RotationSpeed = settings.RotationSpeed;
        _characterStats.GravityMultiplier = settings.GravityMultiplier;
        _characterStats.GroundStickAcceleration = settings.GroundStickAcceleration;
        _characterStats.CoyoteTime = settings.CoyoteTime;
        _characterStats.BunnyHopResetDelay = settings.BunnyHopResetDelay;
        _characterStats.BunnyHopSpeedBonusPerJump = settings.BunnyHopSpeedBonusPerJump;
        _characterStats.MaxBunnyHopSpeedBonus = settings.MaxBunnyHopSpeedBonus;
        _characterStats.BunnyHopCameraAlignment = settings.BunnyHopCameraAlignment;
        _characterStats.BunnyHopCameraTurnSlowdownSpeed =
            settings.BunnyHopCameraTurnSlowdownSpeed;
        _characterStats.BunnyHopCameraTurnSlowdownStrength =
            settings.BunnyHopCameraTurnSlowdownStrength;
        _characterStats.DefaultAirAcceleration = settings.DefaultAirAcceleration;
        _characterStats.AirTurnSpeed = settings.AirTurnSpeed;
        _characterStats.DefaultAirDeceleration = settings.DefaultAirDeceleration;
        _characterStats.LandingSlideDuration = settings.LandingSlideDuration;
        _characterStats.LandingSlideDeceleration = settings.LandingSlideDeceleration;
        _characterStats.LandingSlideSpeedMultiplier = settings.LandingSlideSpeedMultiplier;
        _characterStats.LandingSlideInputCarry = settings.LandingSlideInputCarry;

        _statModifierLayer.Reset();
    }
}
