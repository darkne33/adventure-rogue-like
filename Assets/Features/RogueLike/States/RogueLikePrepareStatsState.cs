using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;

public class RogueLikePrepareStatsState : State
{
    private readonly CharacterStats _characterStats;
    private readonly CharacterSettingsConfiguration _characterSettingsConfiguration;

    public RogueLikePrepareStatsState(CharacterStats characterStats,
        CharacterSettingsConfiguration characterSettingsConfiguration)
    {
        _characterStats = characterStats;
        _characterSettingsConfiguration = characterSettingsConfiguration;
    }

    public override UniTask Enter(CancellationToken cts)
    {
        CharacterStatsInitialize();
        return base.Enter(cts);
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
        
        _characterStats.MaxHp = _characterSettingsConfiguration.MaxHp;
        _characterStats.RegenHp = _characterSettingsConfiguration.RegenHp;
        _characterStats.Armor = _characterSettingsConfiguration.Armor;
        _characterStats.Evasion = _characterSettingsConfiguration.Evasion;
        
        _characterStats.GainHp = _characterSettingsConfiguration.GainHp;
        _characterStats.Luck = _characterSettingsConfiguration.Luck;
        _characterStats.GainGold = _characterSettingsConfiguration.GainGold;
        
        _characterStats.MovementSpeed = _characterSettingsConfiguration.MovementSpeed;
        _characterStats.MovementAcceleration = _characterSettingsConfiguration.Acceleration;
        _characterStats.JumpForce = _characterSettingsConfiguration.JumpForce;
        _characterStats.RotationSpeed = _characterSettingsConfiguration.RotationSpeed;
        _characterStats.GravityMultiplier = _characterSettingsConfiguration.GravityMultiplier;
    }
}