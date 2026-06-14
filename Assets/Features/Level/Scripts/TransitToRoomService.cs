using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Enemies.Scripts.Level.Scripts
{
    public class TransitToRoomService : ITransitToRoomService
    {
        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly ICharacterProvider _characterProvider;
        private readonly IGameModeService _gameModeService;
        private readonly IRoomTransitionService _roomTransitionService;

        public TransitToRoomService(IRogueLikeRuntimeDataService runtimeDataService,
            ICharacterProvider characterProvider, IGameModeService gameModeService,
            IRoomTransitionService roomTransitionService)
        {
            _runtimeDataService = runtimeDataService;
            _characterProvider = characterProvider;
            _gameModeService = gameModeService;
            _roomTransitionService = roomTransitionService;
        }

        public void Transit(Room nextRoom, RoomDoor entryDoor)
        {
            if (_roomTransitionService.IsPlaying)
                return;

            if (nextRoom == null)
                throw new System.ArgumentNullException(nameof(nextRoom));

            if (entryDoor == null)
                throw new System.ArgumentNullException(nameof(entryDoor));

            RoomData roomData = nextRoom.RoomData ??
                                throw new System.InvalidOperationException(
                                    $"{nextRoom.name} does not contain room data.");

            if (roomData is not DefaultEnemiesRoomData && roomData is not StartRoomData)
                throw new System.InvalidOperationException(
                    $"Room transition does not support {roomData.GetType().Name}.");

            if (_characterProvider.CharacterFacade == null)
                throw new System.InvalidOperationException("Character is not available for room transition.");

            CloseRoomDoors(_runtimeDataService.CurrentRoomData);
            TransitAsync(roomData, entryDoor).Forget();
        }

        private async UniTask TransitAsync(RoomData roomData, RoomDoor entryDoor)
        {
            await _roomTransitionService.Play(() =>
            {
                _runtimeDataService.SetCurrentRoomData(roomData);

                Transform teleportPlayerTarget = entryDoor.transform;
                const int offset = 10;
                Vector3 characterPosition = teleportPlayerTarget.position + teleportPlayerTarget.forward * offset;

                _characterProvider.CharacterFacade.transform.position = characterPosition;
                entryDoor.Close();

                if (roomData is DefaultEnemiesRoomData)
                {
                    _gameModeService.Get<RogueLikeStateMachine>()
                        .EnterState<RogueLikeRoomPrepareState>().Forget();
                }
                else
                {
                    foreach (RoomDoor roomDoor in roomData.RoomDoors)
                    {
                        if (roomDoor != null)
                            roomDoor.Open();
                    }
                }

                return UniTask.CompletedTask;
            });
        }

        private static void CloseRoomDoors(RoomData roomData)
        {
            if (roomData?.RoomDoors == null)
                return;

            foreach (RoomDoor roomDoor in roomData.RoomDoors)
            {
                if (roomDoor != null)
                    roomDoor.Close();
            }
        }
    }

    public interface ITransitToRoomService
    {
        void Transit(Room nextRoom, RoomDoor entryDoor);
    }
}
