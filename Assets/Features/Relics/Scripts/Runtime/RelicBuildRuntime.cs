using System;
using System.Collections.Generic;
using System.Threading;
using Features.Enemies.Scripts;
using UnityEngine;

namespace Features.Relics.Scripts
{
    // Composition and lifecycle only; item rules and mutable instances belong to their runtimes.
    internal sealed class RelicBuildRuntime : IDisposable
    {
        private readonly ExplosivoRelicRuntime _explosivo;
        private readonly AquariusRelicRuntime _aquarius;
        private readonly SacrificialDaggerRelicRuntime _daggers;

        public RelicBuildModifiers Modifiers { get; }
        public RelicProjectileContext Projectiles { get; }

        public RelicBuildRuntime(IReadOnlyList<RelicRuntimeState> relics, CharacterStats stats,
            CharacterWallet wallet, IEnemiesProvider enemies, CharacterDamageCalculator calculator,
            RelicEventBus events, IRelicVisualEffectService visuals, CancellationToken lifetimeToken,
            Func<int, CombatTarget, float, bool, int> modifyDamage,
            Action<Vector3, float, int, string> dealAreaDamage)
        {
            var combat = new RelicCombatService(stats, calculator, events, modifyDamage, dealAreaDamage);
            Modifiers = new RelicBuildModifiers(relics, stats, wallet);
            Projectiles = new RelicProjectileContext(Modifiers, visuals, lifetimeToken);
            _explosivo = new ExplosivoRelicRuntime(visuals, combat, lifetimeToken);
            _aquarius = new AquariusRelicRuntime(relics, enemies, stats, visuals, combat);
            _daggers = new SacrificialDaggerRelicRuntime(relics, enemies, stats, visuals, combat);
        }

        public void Tick(CharacterFacade character)
        {
            if (character == null || character.HealthSystem.IsDead)
            {
                ClearEffects();
                return;
            }
            if (Time.deltaTime <= 0f)
                return;
            _explosivo.Tick();
            _aquarius.Tick(character);
            _daggers.Tick(character);
        }

        // Chance/cooldown/Chalice rolls stay in RelicManager and happen before this dispatch.
        public bool TryApplyTriggeredEffect(RelicRuntimeState state, RelicEffectDefinition effect,
            RelicTriggerType trigger, object context)
        {
            if (trigger != RelicTriggerType.OnHit || context is not RelicHitEvent hit ||
                RelicBuildEffects.GetId(effect) != RelicBuildEffects.Explosivo)
                return false;
            _explosivo.Attach(state, effect, hit);
            return true;
        }

        public void OnMoveDistance(RelicMoveDistanceEvent moveEvent) => _aquarius.OnMoveDistance(moveEvent);

        public void Remove(RelicRuntimeState state)
        {
            _explosivo.Remove(state);
            _aquarius.Remove(state);
            _daggers.Remove(state);
        }

        public void BeginRoom()
        {
            Projectiles.BeginRoom();
            ClearEffects();
        }

        public void ClearEffects()
        {
            _explosivo.Clear();
            _aquarius.Clear();
            _daggers.Clear();
        }

        public void Dispose()
        {
            ClearEffects();
            Modifiers.Reset();
        }
    }
}
