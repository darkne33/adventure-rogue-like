using System.Collections.Generic;
using Core;

namespace Features.Enemies.Scripts
{
    public class EnemiesWaveObserver
    {
        public int CurrentWave { get; private set; }

        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly IGameModeService _gameModeService;
        private readonly LevelsConfiguration _levelsConfiguration;

        public EnemiesWaveObserver(IRogueLikeRuntimeDataService runtimeDataService,
            IGameModeService gameModeService, LevelsConfiguration levelsConfiguration)
        {
            _runtimeDataService = runtimeDataService;
            _gameModeService = gameModeService;
            _levelsConfiguration = levelsConfiguration;
        }

        public void Observe(List<EnemyFacade> enemies)
        {
            DefaultEnemiesRoomData currentRoomData = (DefaultEnemiesRoomData)_runtimeDataService.CurrentRoomData;

            var lastCurrentWaveIndex = currentRoomData.EnemyWavesConfiguration.Length - 1;

            if (enemies.Count == 0 && CurrentWave < lastCurrentWaveIndex)
            {
                CurrentWave++;

                var rogueLikeStateMachine = _gameModeService.Get<RogueLikeStateMachine>();
                rogueLikeStateMachine.EnterState<RogueLikeSpawnEnemyWaveState>();
            }
        }
    }
}