using System;
using System.Collections.Generic;
using Zenject;

public class AbilityChoiceProvider : IAbilityChoiceProvider
{
    private readonly AllAbilitiesConfiguration _allAbilitiesConfiguration;
    private readonly DiContainer _container;

    private readonly Dictionary<AbilityName, CharacterAbility> _characterAbilities = new();

    public AbilityChoiceProvider(AllAbilitiesConfiguration allAbilitiesConfiguration, DiContainer container)
    {
        _allAbilitiesConfiguration = allAbilitiesConfiguration;
        _container = container;
    }

    public void CreateAllAbilities()
    {
        foreach (var abilityConfig in _allAbilitiesConfiguration.Abilities)
        {
            SingleShootAbility ability;

            switch (abilityConfig.AbilityName)
            {
                case AbilityName.FireBall:
                    ability = _container.Instantiate<SingleShootAbility>();
                    ability.Initialize(abilityConfig);
                    _characterAbilities.Add(AbilityName.FireBall, ability);
                    break;
                case AbilityName.RabbitBoomerang:
                    ability = _container.Instantiate<SingleShootAbility>();
                    ability.Initialize(abilityConfig);
                    _characterAbilities.Add(AbilityName.RabbitBoomerang, ability);
                    break;
                default:
                    throw new Exception("Unknown ability name: " + abilityConfig.AbilityName);
            }
        }
    }

    public CharacterAbility GetAbility(AbilityName abilityName) =>
        _characterAbilities.GetValueOrDefault(abilityName);
}