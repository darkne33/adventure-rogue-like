using System;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    public sealed class BossSystemsFactory : IBossSystemsFactory
    {
        private readonly ICharacterProvider _characterProvider;
        private readonly IEnemiesProvider _enemiesProvider;
        private readonly CharacterStats _characterStats;
        private readonly RelicManager _relicManager;
        private readonly GoldDropper _goldDropper;
        private readonly ExpDropper _expDropper;

        public BossSystemsFactory(ICharacterProvider characterProvider, IEnemiesProvider enemiesProvider,
            CharacterStats characterStats, RelicManager relicManager, GoldDropper goldDropper, ExpDropper expDropper)
        {
            _characterProvider = characterProvider;
            _enemiesProvider = enemiesProvider;
            _characterStats = characterStats;
            _relicManager = relicManager;
            _goldDropper = goldDropper;
            _expDropper = expDropper;
        }

        public void Create(BossFacade facade)
        {
            BossConfig configuration = facade.Config;
            if (configuration == null)
                throw new InvalidOperationException($"BossConfig is missing on {facade.name}.");
            if (facade is MushroomBossFacade mushroomBoss && mushroomBoss.MovementConfiguration == null)
                throw new InvalidOperationException($"MushroomBossConfiguration is missing on {facade.name}.");

            CharacterFacade character = _characterProvider.CharacterFacade;
            if (character == null)
                throw new InvalidOperationException($"The character is not available for boss {facade.name}.");

            Rigidbody rigidbody = facade.GetComponent<Rigidbody>();
            Renderer[] meshRenderers = facade.MeshRenderers;
            if (meshRenderers == null || meshRenderers.Length == 0)
                meshRenderers = facade.GetComponentsInChildren<Renderer>(true);

            IBossAnimationSystem animationSystem = CreateAnimationSystem(facade);
            var effectsSystem = new DealDamageEffectSystem(
                meshRenderers, useWhiteHitFlash: true, hitColorOverride: Color.white);
            var deathSystem = new BossDeathSystem(facade, _enemiesProvider, character,
                _characterStats, _goldDropper, _expDropper);
            // Split children receive their parent's health immediately after Initialize;
            // never take another build snapshot during the same encounter.
            int maxHealth = facade is MushroomBossFacade { IsSplitChild: true }
                ? Mathf.Max(1, configuration.MaxHealth)
                : configuration.GetScaledMaxHealth(character.CharacterAbilitySystem.CalculateEstimatedDps() *
                    _relicManager.GetEstimatedBossDamageMultiplier());
            var healthSystem = new HealthSystem(maxHealth,
                facade.GetComponents<IHealthView>(), deathSystem, facade.GetComponents<IDamageView>());
            BossAttackSystem attackSystem = facade is MushroomBossFacade mushroom
                ? new MushroomBossAttackSystem(mushroom, character)
                : new BossAttackSystem(facade, character);
            var combatSystem = new BossCombatSystem(facade, attackSystem, animationSystem);

            facade.Construct(rigidbody, meshRenderers, healthSystem, animationSystem,
                attackSystem, combatSystem, effectsSystem);
        }

        private static IBossAnimationSystem CreateAnimationSystem(BossFacade facade) =>
            facade switch
            {
                WoodGuardBossFacade woodGuard => new WoodGuardBossAnimation(
                    woodGuard.Animator, woodGuard.AttackClip, woodGuard.Config.AnimationSpeedBlendDuration),
                MushroomBossFacade mushroom => new MushroomBossAnimation(
                    mushroom.Animator, mushroom.JumpClip, mushroom.HeadClip),
                _ => throw new ArgumentOutOfRangeException(nameof(facade), facade.GetType(),
                    "Boss animation type is not supported.")
            };
    }
}
