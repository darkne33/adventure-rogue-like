using System;
using System.Collections.Generic;
using Zenject;

public class CharacterAbilityChoiceProvider : IAbilityChoiceProvider
{
    private readonly AllAbilitiesConfiguration _allAbilitiesConfiguration;
    private readonly DiContainer _container;

    private readonly Dictionary<AbilityName, CharacterAbility> _characterAbilities = new();

    public CharacterAbilityChoiceProvider(AllAbilitiesConfiguration allAbilitiesConfiguration, DiContainer container)
    {
        _allAbilitiesConfiguration = allAbilitiesConfiguration;
        _container = container;
    }

    public void CreateAllAbilities()
    {
        foreach (var abilityConfig in _allAbilitiesConfiguration.Abilities)
        {
            switch (abilityConfig.AbilityName)
            {
                case AbilityName.FireBall:
                    CreateAbility<FireballAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.RabbitBoomerang:
                    CreateAbility<RabbitBoomerangAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.FireField:
                    CreateAbility<FireFieldAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.EarthRock:
                    CreateAbility<EarthRockAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.BulletExplosion:
                    CreateAbility<BulletExplosionAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.Punch:
                    CreateAbility<PunchAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.AbilityDurationScroll:
                    CreateAbility<CharacterScrollAbilityDurationAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.ArmorScroll:
                    CreateAbility<CharacterScrollArmorAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.AttackSpeedScroll:
                    CreateAbility<CharacterScrollAttackSpeedAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.CritChanceScroll:
                    CreateAbility<CharacterScrollCritChanceAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.CritDamageScroll:
                    CreateAbility<CharacterScrollCritDamageAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.DamageScroll:
                    CreateAbility<CharacterScrollDamageAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.EvasionScroll:
                    CreateAbility<CharacterScrollEvasionAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.GainGoldScroll:
                    CreateAbility<CharacterScrollGainGoldAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.LifeStealScroll:
                    CreateAbility<CharacterScrollLifeStealAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.LuckScroll:
                    CreateAbility<CharacterScrollLuckAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.MaxHpScroll:
                    CreateAbility<CharacterScrollMaxHpAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.MovementSpeedScroll:
                    CreateAbility<CharacterScrollMovementSpeedAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.RegenHpScroll:
                    CreateAbility<CharacterScrollRegenHpAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.ThornsDamageScroll:
                    CreateAbility<CharacterScrollThornsDamageAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                case AbilityName.ShieldScroll:
                    CreateAbility<CharacterScrollShieldAbility>(abilityConfig, abilityConfig.AbilityName);
                    break;
                default:
                    throw new Exception("Unknown ability name: " + abilityConfig.AbilityName);
            }
        }
    }

    private void CreateAbility<T>(AbilityConfiguration abilityConfig, AbilityName abilityName) where T : CharacterAbility
    {
        CharacterAbility ability = _container.Instantiate<T>();
        ability.Initialize(abilityConfig);
        _characterAbilities.Add(abilityName, ability);
    }

    public CharacterAbility GetAbility(AbilityName abilityName) =>
        _characterAbilities.GetValueOrDefault(abilityName);

    public Dictionary<AbilityName, CharacterAbility> GetCharacterAbilities() => _characterAbilities;
}
