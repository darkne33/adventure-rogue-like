using System;
using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public sealed class EnemySystemsFactory : IEnemySystemsFactory
    {
        private readonly ICharacterProvider _characterProvider;
        private readonly IEnemiesProvider _enemiesProvider;
        private readonly CharacterStats _characterStats;
        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly LevelsConfiguration _levelsConfiguration;
        private readonly GoldDropper _goldDropper;
        private readonly ExpDropper _expDropper;

        public EnemySystemsFactory(ICharacterProvider characterProvider, IEnemiesProvider enemiesProvider,
            CharacterStats characterStats, IRogueLikeRuntimeDataService runtimeDataService,
            LevelsConfiguration levelsConfiguration,
            GoldDropper goldDropper, ExpDropper expDropper)
        {
            _characterProvider = characterProvider;
            _enemiesProvider = enemiesProvider;
            _characterStats = characterStats;
            _runtimeDataService = runtimeDataService;
            _levelsConfiguration = levelsConfiguration;
            _goldDropper = goldDropper;
            _expDropper = expDropper;
        }

        public void Create(EnemyFacade facade)
        {
            ConfigureRoomStats(facade);
            CharacterFacade character = _characterProvider.CharacterFacade;
            EnemyConfiguration configuration = facade.Configuration;
            Rigidbody rigidbody = facade.GetComponent<Rigidbody>();
            NavMeshAgent navMeshAgent = facade.GetComponent<NavMeshAgent>();
            Animator animator = facade.GetComponent<Animator>();
            EnemyCollisionDetector collisionDetector = facade.GetComponent<EnemyCollisionDetector>();
            EnemyAggroIndicatorView aggroIndicatorView =
                facade.GetComponent<EnemyAggroIndicatorView>() ??
                facade.gameObject.AddComponent<EnemyAggroIndicatorView>();

            IEnemyAnimationSystem animationSystem = CreateAnimationSystem(configuration, animator);
            IEnemyMovementSystem movementSystem = configuration.EnemyDamageType is EnemyDamageType.Dash or EnemyDamageType.Jump
                ? null
                : CreateMovementSystem(configuration, facade, character, navMeshAgent, animationSystem);
            float attackPreparationDuration = configuration.AttackPreparationDuration;
            IEnemyDamageSystem damageSystem = CreateDamageSystem(configuration, facade, character,
                facade.GetComponent<EnemyDashView>(), facade.GetComponent<EnemyRangedAttackView>(),
                attackPreparationDuration);
            if (damageSystem is IEnemyMovementSystem attackMovementSystem)
                movementSystem = attackMovementSystem;

            var effectsSystem = new DealDamageEffectSystem(
                facade.MeshRenderers, facade.AttackTelegraphTransform, useWhiteHitFlash: true);
            Action deathEffect = configuration.ExplodesOnDeath &&
                                 damageSystem is EnemyDamageAreaSystem areaDamageSystem
                ? areaDamageSystem.DetonateOnDeath
                : null;
            BombEnemySplitOnDeath splitOnDeath = facade.GetComponent<BombEnemySplitOnDeath>();
            if (splitOnDeath != null &&
                !_levelsConfiguration.EnemyRoomScalingConfiguration.UsesOnlyElites(
                    _runtimeDataService.CurrentIndexLevel))
                deathEffect += splitOnDeath.SpawnNormalBombs;

            var deathSystem = new EnemyDeathSystem(_enemiesProvider, facade, configuration, _characterStats,
                character, effectsSystem, deathEffect, _goldDropper, _expDropper);
            var healthSystem = new HealthSystem(configuration.MaxHealth,
                new IHealthView[] { facade.GetComponent<EnemyHealthView>() }, deathSystem,
                new IDamageView[] { facade.GetComponent<EnemyDamageNumberView>() });

            facade.Construct(rigidbody, navMeshAgent, collisionDetector, animationSystem, movementSystem,
                damageSystem, healthSystem, effectsSystem, aggroIndicatorView);
        }

        private IEnemyAnimationSystem CreateAnimationSystem(EnemyConfiguration configuration,
            Animator animator) =>
            configuration.EnemyAnimationType switch
            {
                EnemyAnimationType.Bun => new BunEnemyAnimation(animator),
                EnemyAnimationType.Dummy => new DummyEnemyAnimation(animator),
                EnemyAnimationType.Skeleton => new SkeletonEnemyAnimation(animator),
                EnemyAnimationType.Ghost => new GhostEnemyAnimation(animator),
                EnemyAnimationType.Bomb => new BombEnemyAnimation(animator),
                EnemyAnimationType.Empty => new EmptyEnemyAnimation(),
                _ => throw new ArgumentOutOfRangeException(nameof(configuration.EnemyAnimationType),
                    configuration.EnemyAnimationType, "Enemy animation type is not supported.")
            };

        private IEnemyDamageSystem CreateDamageSystem(EnemyConfiguration configuration, EnemyFacade facade,
            CharacterFacade character, EnemyDashView dashView, EnemyRangedAttackView rangedAttackView,
            float attackPreparationDuration) =>
            configuration.EnemyDamageType switch
            {
                EnemyDamageType.Melee => new EnemyDamageMeleeSystem(
                    facade, character, configuration, attackPreparationDuration),
                EnemyDamageType.Dash => new EnemyDashAttackSystem(
                    character, configuration, facade, dashView, attackPreparationDuration, _enemiesProvider),
                EnemyDamageType.Jump => new EnemyJumpAttackSystem(
                    facade, character, configuration, _enemiesProvider),
                EnemyDamageType.RangeArea => new EnemyDamageAreaSystem(
                    character, configuration, facade,
                    _enemiesProvider,
                    facade.GetComponent<EnemyAreaDamageIndicatorView>(),
                    attackPreparationDuration),
                EnemyDamageType.RangeDirection => new EnemyRangedAttackSystem(
                    character, configuration, facade, rangedAttackView, attackPreparationDuration),
                EnemyDamageType.RangeBullet => new EnemyBulletAttackSystem(
                    character, configuration, facade, attackPreparationDuration),
                _ => throw new ArgumentOutOfRangeException(nameof(configuration.EnemyDamageType),
                    configuration.EnemyDamageType, "Enemy damage type is not supported.")
            };

        private IEnemyMovementSystem CreateMovementSystem(EnemyConfiguration configuration, EnemyFacade facade,
            CharacterFacade character, NavMeshAgent navMeshAgent, IEnemyAnimationSystem animationSystem)
        {
            // Roll once while constructing this spawn's movement system.
            if (configuration.RandomizeSkirmisherOnSpawn && UnityEngine.Random.Range(0, 2) == 1)
            {
                return new EnemyAttackRangeSkirmisherMovementSystem(
                    facade, character, configuration, navMeshAgent, animationSystem, _enemiesProvider);
            }

            return configuration.EnemyMovementType switch
            {
                EnemyMovementType.Chase => new EnemyChaseMovementSystem(
                    facade, character, configuration, navMeshAgent, animationSystem),
                EnemyMovementType.Skirmisher => new EnemySkirmisherMovementSystem(
                    facade, character, configuration, navMeshAgent, animationSystem),
                EnemyMovementType.AggressiveChase => new EnemyAggressiveChaseMovementSystem(
                    facade, character, configuration, navMeshAgent, animationSystem),
                EnemyMovementType.Aggressive => new EnemyAggressiveMovementSystem(
                    facade, character, configuration, navMeshAgent, animationSystem),
                EnemyMovementType.RangeChase => new EnemyRangeChaseMovementSystem(
                    facade, character, configuration, navMeshAgent, animationSystem),
                EnemyMovementType.Stationary => new EnemyStationaryMovementSystem(
                    facade, character, animationSystem),
                _ => throw new ArgumentOutOfRangeException(nameof(configuration.EnemyMovementType),
                    configuration.EnemyMovementType, "Enemy movement type is not supported.")
            };
        }

        private void ConfigureRoomStats(EnemyFacade facade)
        {
            if (_runtimeDataService.CurrentRoomData is not DefaultEnemiesRoomData currentRoomData)
                return;

            int roomIndex = _runtimeDataService.GetCombatProgressIndex(currentRoomData);
            facade.ConfigureForRoom(_levelsConfiguration.GetEnemyHealthScalingConfiguration(), roomIndex,
                _levelsConfiguration.ProceduralLevels,
                _levelsConfiguration.GetStatGrowthStep(_runtimeDataService.CurrentIndexLevel));
        }
    }
}
