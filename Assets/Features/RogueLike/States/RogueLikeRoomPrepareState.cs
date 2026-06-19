using System;
using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;

namespace Core
{
    public class RogueLikeRoomPrepareState : State
    {
        private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
        private readonly EnemiesWaveObserver _enemiesWaveObserver;
        private readonly EnemySpawner _enemySpawner;
        private readonly ICharacterProvider _characterProvider;

        public RogueLikeRoomPrepareState(IRogueLikeRuntimeDataService rogueLikeRuntimeDataService,
            EnemiesWaveObserver enemiesWaveObserver, EnemySpawner enemySpawner,
            ICharacterProvider characterProvider)
        {
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _enemiesWaveObserver = enemiesWaveObserver;
            _enemySpawner = enemySpawner;
            _characterProvider = characterProvider;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            var currentRoomData = _rogueLikeRuntimeDataService.CurrentRoomData;

            if (currentRoomData is not DefaultEnemiesRoomData)
                throw new InvalidOperationException(
                    $"Room prepare state does not support {currentRoomData?.GetType().Name ?? "null"} room data.");

            if (currentRoomData.RoomDoors == null)
                throw new InvalidOperationException("Room doors are not configured.");

            if (_enemiesWaveObserver.RestoreCompletedRoom())
                return;

            _enemiesWaveObserver.StartRoom();
            await _enemySpawner.LoadEnemyPrefabs(cts);

            foreach (var roomDoor in currentRoomData.RoomDoors)
            {
                if (roomDoor != null)
                    roomDoor.Close();
            }

            _enemySpawner.TrySpawnEnemies(_characterProvider.CharacterFacade,
                _enemiesWaveObserver.CurrentWave);
        }
    }
}
