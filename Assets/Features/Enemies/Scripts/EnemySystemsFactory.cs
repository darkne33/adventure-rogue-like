using System;
using Core;
using UnityEngine;
using UnityEngine.AI;

namespace Features.Enemies.Scripts
{
    public sealed class EnemySystemsFactory : IEnemySystemsFactory
    {
        private readonly ICharacterProvider _characterProvider;
        private readonly IEnemiesProvider _enemiesProvider;
        private readonly ICharacterLevelService _characterLevelService;
        private readonly CharacterStats _characterStats;
        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly ISceneService<RogueLikeSceneProvider> _sceneService;
        private readonly LevelsConfiguration _levelsConfiguration;
        private readonly GoldDropper _goldDropper;
        private readonly HeartDropper _heartDropper;

        public EnemySystemsFactory(ICharacterProvider characterProvider, IEnemiesProvider enemiesProvider,
            ICharacterLevelService characterLevelService, CharacterStats characterStats,
            IRogueLikeRuntimeDataService runtimeDataService, ISceneService<RogueLikeSceneProvider> sceneService,
            LevelsConfiguration levelsConfiguration, GoldDropper goldDropper, HeartDropper heartDropper)
        {
            _characterProvider = characterProvider;
            _enemiesProvider = enemiesProvider;
            _characterLevelService = characterLevelService;
            _characterStats = characterStats;
            _runtimeDataService = runtimeDataService;
            _sceneService = sceneService;
            _levelsConfiguration = levelsConfiguration;
            _goldDropper = goldDropper;
            _heartDropper = heartDropper;
        }

        public void Create(EnemyFacade facade)
        {
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
            IEnemyMovementSystem movementSystem = CreateMovementSystem(configuration, facade, character,
                navMeshAgent, animationSystem);
            float attackPreparationDuration = configuration.AttackPreparationDuration;
            IEnemyDamageSystem damageSystem = CreateDamageSystem(configuration, facade, character,
                facade.GetComponent<EnemyDashView>(), facade.GetComponent<EnemyRangedAttackView>(),
                attackPreparationDuration);
            var effectsSystem = new DealDamageEffectSystem(
                facade.MeshRenderers, facade.AttackTelegraphTransform);
            var deathSystem = new EnemyDeathSystem(_enemiesProvider, facade, _characterLevelService, configuration,
                _characterStats, character, effectsSystem, _goldDropper, _heartDropper);
            int maxHealth = GetScaledMaxHealth(configuration.MaxHealth);
            var healthSystem = new HealthSystem(maxHealth,
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
                    character, configuration, facade, dashView, attackPreparationDuration),
                EnemyDamageType.RangeArea => new EnemyRangedAttackSystem(
                    character, configuration, facade, rangedAttackView, attackPreparationDuration),
                EnemyDamageType.RangeDirection => new EnemyRangedAttackSystem(
                    character, configuration, facade, rangedAttackView, attackPreparationDuration),
                EnemyDamageType.RangeBullet => new EnemyBulletAttackSystem(
                    character, configuration, facade, attackPreparationDuration),
                _ => throw new ArgumentOutOfRangeException(nameof(configuration.EnemyDamageType),
                    configuration.EnemyDamageType, "Enemy damage type is not supported.")
            };

        private IEnemyMovementSystem CreateMovementSystem(EnemyConfiguration configuration, EnemyFacade facade,
            CharacterFacade character, NavMeshAgent navMeshAgent, IEnemyAnimationSystem animationSystem) =>
            configuration.EnemyMovementType switch
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
                _ => throw new ArgumentOutOfRangeException(nameof(configuration.EnemyMovementType),
                    configuration.EnemyMovementType, "Enemy movement type is not supported.")
            };

        private int GetScaledMaxHealth(int baseHealth)
        {
            if (_runtimeDataService.CurrentRoomData is not DefaultEnemiesRoomData currentRoomData)
                return baseHealth;

            LevelView currentLevel = _sceneService.GameSceneComponentsService?.CurrentLevel;
            if (currentLevel == null)
                throw new InvalidOperationException("Current level view is not available.");

            int roomIndex = currentLevel.GetEnemyRoomIndex(currentRoomData);
            return _levelsConfiguration.GetEnemyHealthScalingConfiguration()
                .GetMaxHealth(baseHealth, _runtimeDataService.CurrentIndexLevel, roomIndex);
        }
    }
}
