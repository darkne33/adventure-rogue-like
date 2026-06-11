using System;
using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;

namespace Core
{
    public class RogueLikeRoomPrepareState : State
    {
        private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;

        public RogueLikeRoomPrepareState(IRogueLikeRuntimeDataService rogueLikeRuntimeDataService)
        {
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            var currentRoomData = _rogueLikeRuntimeDataService.CurrentRoomData;

            if (currentRoomData is not DefaultEnemiesRoomData)
                throw new InvalidOperationException(
                    $"Room prepare state does not support {currentRoomData?.GetType().Name ?? "null"} room data.");

            if (currentRoomData.RoomDoors == null)
                throw new InvalidOperationException("Room doors are not configured.");

            foreach (var roomDoor in currentRoomData.RoomDoors)
                roomDoor.Close();

            await StateMachine.EnterState<RogueLikeSpawnEnemyWaveState>();
        }
    }
}
