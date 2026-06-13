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

        public RogueLikeRoomPrepareState(IRogueLikeRuntimeDataService rogueLikeRuntimeDataService,
            EnemiesWaveObserver enemiesWaveObserver)
        {
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _enemiesWaveObserver = enemiesWaveObserver;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            var currentRoomData = _rogueLikeRuntimeDataService.CurrentRoomData;

            if (currentRoomData is not DefaultEnemiesRoomData)
                throw new InvalidOperationException(
                    $"Room prepare state does not support {currentRoomData?.GetType().Name ?? "null"} room data.");

            if (currentRoomData.RoomDoors == null)
                throw new InvalidOperationException("Room doors are not configured.");

            _enemiesWaveObserver.StartRoom();

            foreach (var roomDoor in currentRoomData.RoomDoors)
                roomDoor.Close();

            await StateMachine.EnterState<RogueLikeSpawnEnemyWaveState>();
        }
    }
}
