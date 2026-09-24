using System;
using Features.Enemies.Scripts;
using UnityEngine;

namespace Features.Relics.Scripts
{
    // Adapts new mechanics to the established outgoing-damage and area-damage pipelines.
    internal sealed class RelicCombatService
    {
        private readonly CharacterStats _stats;
        private readonly CharacterDamageCalculator _calculator;
        private readonly RelicEventBus _events;
        private readonly Func<int, CombatTarget, float, bool, int> _modifyDamage;
        private readonly Action<Vector3, float, int, string> _dealAreaDamage;

        public RelicCombatService(CharacterStats stats, CharacterDamageCalculator calculator,
            RelicEventBus events, Func<int, CombatTarget, float, bool, int> modifyDamage,
            Action<Vector3, float, int, string> dealAreaDamage)
        {
            _stats = stats;
            _calculator = calculator;
            _events = events;
            _modifyDamage = modifyDamage;
            _dealAreaDamage = dealAreaDamage;
        }

        public void DealAreaDamage(Vector3 position, float radius, int damage, string sourceId) =>
            _dealAreaDamage(position, radius, damage, sourceId);

        public void DealPeriodicDamage(CharacterFacade character, CombatTarget target, float baseDamage,
            float travelDistance, string sourceId)
        {
            float multiplier = (1f + Mathf.Max(-90f, _stats.DamageInPercent) * 0.01f) *
                               _stats.RelicDamageMultiplier;
            int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
            damage = _modifyDamage(damage, target, travelDistance, false);
            int applied = target.HealthSystem.GetDamage(damage);
            if (applied > 0)
                target.EffectsSystem.DealDamage(0.04f);
            if (applied > 0 && target.IsDead)
                _events.PublishKill(new RelicKillEvent(character, target, target.transform.position, sourceId));
        }

        public void DealDirectHit(CharacterFacade character, CombatTarget target, int baseDamage, string sourceId)
        {
            CharacterDamageResult roll = _calculator.Calculate(baseDamage);
            int damage = _modifyDamage(roll.Damage, target, -1f, true);
            int applied = target.HealthSystem.GetDamage(damage, roll.IsCritical);
            if (applied <= 0)
                return;
            bool killed = target.IsDead;
            target.EffectsSystem.DealDamage(0.05f);
            float healing = applied * Mathf.Max(0f, _stats.LifeSteal) * 0.01f;
            if (healing > 0f)
            {
                float healed = character.HealthSystem.IncreaseCurrentHealth(healing);
                if (healed > 0f)
                    _events.PublishHeal(new RelicHealEvent(character, healed));
            }
            _events.PublishHit(new RelicHitEvent(character, target, damage, roll.IsCritical,
                sourceId, GetTargetPosition(target), null, applied));
            if (killed)
                _events.PublishKill(new RelicKillEvent(character, target, target.transform.position, sourceId));
        }

        public static Vector3 GetTargetPosition(CombatTarget target) =>
            target.TargetToShootDamage != null ? target.TargetToShootDamage.position :
                target.transform.position + Vector3.up * 0.5f;
    }
}
