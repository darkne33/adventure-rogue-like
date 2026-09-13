using System;
using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Features.Bosses.Scripts;
using Features.Enemies.Scripts;

namespace Core
{
    public class RogueLikeRoomPrepareState : State
    {
        private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
        private readonly EnemyRoomObserver _enemyRoomObserver;
        private readonly EnemySpawner _enemySpawner;
        private readonly BossSpawner _bossSpawner;
        private readonly ICharacterProvider _characterProvider;

        public RogueLikeRoomPrepareState(IRogueLikeRuntimeDataService rogueLikeRuntimeDataService,
            EnemyRoomObserver enemyRoomObserver, EnemySpawner enemySpawner,
            ICharacterProvider characterProvider, BossSpawner bossSpawner)
        {
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _enemyRoomObserver = enemyRoomObserver;
            _enemySpawner = enemySpawner;
            _bossSpawner = bossSpawner;
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
            if (currentRoomData is not BossRoomData)
                await _enemySpawner.LoadEnemyPrefabs(cts);

            foreach (RoomDoor roomDoor in currentRoomData.RoomDoors)
            {
                if (roomDoor != null)
                    roomDoor.Close();
            }

            if (currentRoomData is BossRoomData)
                _bossSpawner.SpawnBoss();
            else
                _enemySpawner.TrySpawnEnemies(_characterProvider.CharacterFacade);
        }
    }
}
