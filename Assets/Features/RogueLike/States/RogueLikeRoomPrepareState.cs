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
        private readonly EnemyRoomObserver _enemyRoomObserver;
        private readonly EnemySpawner _enemySpawner;
        private readonly ICharacterProvider _characterProvider;

        public RogueLikeRoomPrepareState(IRogueLikeRuntimeDataService rogueLikeRuntimeDataService,
            EnemyRoomObserver enemyRoomObserver, EnemySpawner enemySpawner,
            ICharacterProvider characterProvider)
        {
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _enemyRoomObserver = enemyRoomObserver;
            _enemySpawner = enemySpawner;
            _characterProvider = characterProvider;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            if (_rogueLikeRuntimeDataService.CurrentRoomData is not DefaultEnemiesRoomData currentRoomData)
                throw new InvalidOperationException(
                    "Room prepare state supports only default enemies room data.");

            if (currentRoomData.RoomDoors == null)
                throw new InvalidOperationException("Room doors are not configured.");

            if (_enemyRoomObserver.RestoreCompletedRoom())
                return;

            _enemyRoomObserver.StartRoom(waitForEnemySpawning: true);
            await _enemySpawner.LoadEnemyPrefabs(cts);

            foreach (RoomDoor roomDoor in currentRoomData.RoomDoors)
            {
                if (roomDoor != null)
                    roomDoor.Close();
            }

            _enemySpawner.TrySpawnEnemies(_characterProvider.CharacterFacade);
        }
    }
}
