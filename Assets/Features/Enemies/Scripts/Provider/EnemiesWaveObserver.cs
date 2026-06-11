using System.Collections.Generic;
using Core;

namespace Features.Enemies.Scripts
{
    public class EnemiesWaveObserver
    {
        public int CurrentWave { get; private set; }

        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly IGameModeService _gameModeService;
        public EnemiesWaveObserver(IRogueLikeRuntimeDataService runtimeDataService,
            IGameModeService gameModeService)
        {
            _runtimeDataService = runtimeDataService;
            _gameModeService = gameModeService;
        }

        public void Observe(List<EnemyFacade> enemies)
        {
            if (enemies == null)
                throw new System.ArgumentNullException(nameof(enemies));

            if (_runtimeDataService.CurrentRoomData is not DefaultEnemiesRoomData currentRoomData)
                throw new System.InvalidOperationException(
                    "Enemy waves can only be observed in a default enemies room.");

            if (currentRoomData.EnemyWavesConfiguration == null ||
                currentRoomData.EnemyWavesConfiguration.Length == 0)
                throw new System.InvalidOperationException("Enemy waves are not configured for the current room.");

            int lastCurrentWaveIndex = currentRoomData.EnemyWavesConfiguration.Length - 1;

            if (enemies.Count == 0 && CurrentWave < lastCurrentWaveIndex)
            {
                CurrentWave++;

                var rogueLikeStateMachine = _gameModeService.Get<RogueLikeStateMachine>();
                rogueLikeStateMachine.EnterState<RogueLikeSpawnEnemyWaveState>();
            }
        }
    }
}
