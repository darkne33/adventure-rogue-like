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
            
            foreach (var roomDoor in currentRoomData.RoomDoors) 
                roomDoor.Close();

            switch (currentRoomData)
            {
                case DefaultEnemiesRoomData defaultRoomData:
                    await StateMachine.EnterState<RogueLikeSpawnEnemyWaveState>();
                    break;
                default:
                    throw new Exception("Incorrect room data");
            }
        }
    }
}