using System;
using UnityEngine;

namespace Features.RunResults.Scripts
{
    public sealed class RunWeaponResult
    {
        public CharacterActiveAbility Ability { get; }
        public string Name { get; }
        public Sprite Icon { get; }
        public int Level { get; }
        public long Damage { get; }
        public float ActiveSeconds { get; }
        public double Dps => ActiveSeconds > 0f ? Damage / (double)ActiveSeconds : 0d;

        public RunWeaponResult(CharacterActiveAbility ability, int level, long damage,
            float activeSeconds)
        {
            Ability = ability;
            Name = string.IsNullOrWhiteSpace(ability.DisplayName)
                ? ability.Id.ToString()
                : ability.DisplayName;
            Icon = ability.Icon;
            Level = Math.Max(1, level);
            Damage = Math.Max(0L, damage);
            ActiveSeconds = Mathf.Max(0f, activeSeconds);
        }
    }
}
