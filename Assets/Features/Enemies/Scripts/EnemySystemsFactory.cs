using System;
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

        public EnemySystemsFactory(ICharacterProvider characterProvider, IEnemiesProvider enemiesProvider,
            ICharacterLevelService characterLevelService, CharacterStats characterStats)
        {
            _characterProvider = characterProvider;
            _enemiesProvider = enemiesProvider;
            _characterLevelService = characterLevelService;
            _characterStats = characterStats;
        }

        public void Create(EnemyFacade facade)
        {
            CharacterFacade character = _characterProvider.CharacterFacade;
            EnemyConfiguration configuration = facade.Configuration;
            Rigidbody rigidbody = facade.GetComponent<Rigidbody>();
            NavMeshAgent navMeshAgent = facade.GetComponent<NavMeshAgent>();
            Animator animator = facade.GetComponent<Animator>();
            EnemyCollisionDetector collisionDetector = facade.GetComponent<EnemyCollisionDetector>();

            IEnemyAnimationSystem animationSystem = CreateAnimationSystem(configuration, animator);
            IEnemyMovementSystem movementSystem = CreateMovementSystem(configuration, facade, character,
                navMeshAgent, animationSystem);
            IEnemyDamageSystem damageSystem = CreateDamageSystem(configuration, facade, character,
                facade.GetComponent<EnemyDashView>());
            var deathSystem = new EnemyDeathSystem(_enemiesProvider, facade, _characterLevelService, configuration,
                _characterStats, character);
            var healthSystem = new HealthSystem(configuration.MaxHealth,
                new IHealthView[] { facade.GetComponent<EnemyHealthView>() }, deathSystem,
                new IDamageView[] { facade.GetComponent<EnemyDamageNumberView>() });
            var effectsSystem = new DealDamageEffectSystem(facade.MeshRenderers);

            facade.Construct(rigidbody, navMeshAgent, collisionDetector, animationSystem, movementSystem,
                damageSystem, healthSystem, effectsSystem);
        }

        private IEnemyAnimationSystem CreateAnimationSystem(EnemyConfiguration configuration,
            Animator animator) =>
            configuration.EnemyAnimationType switch
            {
                EnemyAnimationType.Bun => new BunEnemyAnimation(animator),
                EnemyAnimationType.Dummy => new DummyEnemyAnimation(animator),
                _ => throw new ArgumentOutOfRangeException(nameof(configuration.EnemyAnimationType),
                    configuration.EnemyAnimationType, "Enemy animation type is not supported.")
            };

        private IEnemyDamageSystem CreateDamageSystem(EnemyConfiguration configuration, EnemyFacade facade,
            CharacterFacade character, EnemyDashView dashView) =>
            configuration.EnemyDamageType switch
            {
                EnemyDamageType.Melee => new EnemyDamageMeleeSystem(facade, character, configuration),
                EnemyDamageType.Dash => new EnemyDashAttackSystem(character, configuration, facade, dashView),
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
                _ => throw new ArgumentOutOfRangeException(nameof(configuration.EnemyMovementType),
                    configuration.EnemyMovementType, "Enemy movement type is not supported.")
            };
    }
}
