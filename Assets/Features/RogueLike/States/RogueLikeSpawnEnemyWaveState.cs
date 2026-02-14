using System;
using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;

namespace Core
{
    public class RogueLikeSpawnEnemyWaveState : State
    {
        private readonly LevelsConfiguration _levelsConfiguration;
        private readonly EnemySpawner _enemySpawner;
        private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
        private readonly ICharacterProvider _characterProvider;
        private readonly EnemiesWaveObserver _enemiesWaveObserver;

        public RogueLikeSpawnEnemyWaveState(LevelsConfiguration levelsConfiguration, EnemySpawner enemySpawner, IRogueLikeRuntimeDataService rogueLikeRuntimeDataService, ICharacterProvider characterProvider, EnemiesWaveObserver enemiesWaveObserver)
        {
            _levelsConfiguration = levelsConfiguration;
            _enemySpawner = enemySpawner;
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _characterProvider = characterProvider;
            _enemiesWaveObserver = enemiesWaveObserver;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            Log.Gameplay.Info("RogueLikeSpawnEnemyWaveState Completed");
            
            foreach (var enemyPrefabData in _levelsConfiguration.Levels[_rogueLikeRuntimeDataService.CurrentIndexLevel]
                         .EnemyFactoryConfiguration.EnemyPrefabs)
                await enemyPrefabData.WavesConfigurationContainer.Load(cts);
            
            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: cts);

            _enemySpawner.TrySpawnEnemies(_characterProvider.CharacterFacade, _enemiesWaveObserver.CurrentWave);
        }
    }
}