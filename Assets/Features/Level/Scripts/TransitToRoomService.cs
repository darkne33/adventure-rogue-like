using Core;
using Cysharp.Threading.Tasks;
using Features.Relics.Scripts;
using UnityEngine;

namespace Features.Enemies.Scripts.Level.Scripts
{
    public class TransitToRoomService : ITransitToRoomService
    {
        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly ICharacterProvider _characterProvider;
        private readonly IGameModeService _gameModeService;
        private readonly IRoomTransitionService _roomTransitionService;
        private readonly RelicEventBus _relicEventBus;
        private bool _isTransitioning;

        public TransitToRoomService(IRogueLikeRuntimeDataService runtimeDataService,
            ICharacterProvider characterProvider, IGameModeService gameModeService,
            IRoomTransitionService roomTransitionService, RelicEventBus relicEventBus)
        {
            _runtimeDataService = runtimeDataService;
            _characterProvider = characterProvider;
            _gameModeService = gameModeService;
            _roomTransitionService = roomTransitionService;
            _relicEventBus = relicEventBus;
        }

        public void Transit(Room nextRoom, RoomDoor entryDoor)
        {
            if (_isTransitioning || _roomTransitionService.IsPlaying)
                return;

            if (nextRoom == null)
                throw new System.ArgumentNullException(nameof(nextRoom));

            if (entryDoor == null)
                throw new System.ArgumentNullException(nameof(entryDoor));

            RoomData roomData = nextRoom.RoomData ??
                                throw new System.InvalidOperationException(
                                    $"{nextRoom.name} does not contain room data.");

            if (roomData is not DefaultEnemiesRoomData &&
                roomData is not RewardRoomData &&
                roomData is not StartRoomData)
                throw new System.InvalidOperationException(
                    $"Room transition does not support {roomData.GetType().Name}.");

            if (_characterProvider.CharacterFacade == null)
                throw new System.InvalidOperationException("Character is not available for room transition.");

            _isTransitioning = true;
            TransitAsync(nextRoom, roomData, entryDoor).Forget();
        }

        private async UniTask TransitAsync(Room nextRoom, RoomData roomData, RoomDoor entryDoor)
        {
            CharacterFacade character = _characterProvider.CharacterFacade;

            try
            {
                character.SetTransitionPaused(true);
                await _roomTransitionService.Play(
                    async () =>
                    {
                        _runtimeDataService.SetCurrentRoomData(roomData);

                        Transform teleportPlayerTarget = entryDoor.transform;
                        const int offset = 10;
                        Vector3 characterPosition = teleportPlayerTarget.position +
                                                    teleportPlayerTarget.forward * offset;

                        TeleportCharacter(characterPosition);
                        entryDoor.Open();
                        _relicEventBus.PublishRoomStarted(
                            new RelicRoomEvent(roomData, nextRoom, characterPosition));

                        if (roomData is DefaultEnemiesRoomData)
                        {
                            await _gameModeService.Get<RogueLikeStateMachine>()
                                .EnterState<RogueLikeRoomPrepareState>();
                        }
                        else if (roomData is RewardRoomData rewardRoomData)
                        {
                            rewardRoomData.MarkCompleted();
                        }

                        if (roomData is RewardRoomData or StartRoomData)
                            OpenRoomDoors(roomData);

                    },
                    () => character.SetTransitionPaused(false));
            }
            finally
            {
                try
                {
                    if (character != null)
                        character.SetTransitionPaused(false);
                }
                finally
                {
                    _isTransitioning = false;
                }
            }
        }

        private void TeleportCharacter(Vector3 position)
        {
            CharacterFacade character = _characterProvider.CharacterFacade;
            if (!character.Rigidbody.isKinematic)
            {
                character.Rigidbody.linearVelocity = Vector3.zero;
                character.Rigidbody.angularVelocity = Vector3.zero;
            }

            character.Rigidbody.position = position;
            character.transform.position = position;
            Physics.SyncTransforms();
        }

        private static void OpenRoomDoors(RoomData roomData)
        {
            if (roomData?.RoomDoors == null)
                return;

            foreach (RoomDoor roomDoor in roomData.RoomDoors)
            {
                if (roomDoor != null)
                    roomDoor.Open();
            }
        }

    }

    public interface ITransitToRoomService
    {
        void Transit(Room nextRoom, RoomDoor entryDoor);
    }
}
