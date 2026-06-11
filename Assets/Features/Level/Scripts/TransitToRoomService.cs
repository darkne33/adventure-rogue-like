using Core;
using Cysharp.Threading.Tasks;

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

        public void Transit(Room nextRoom)
        {
            if (_roomTransitionService.IsPlaying)
                return;

            if (nextRoom == null)
                throw new System.ArgumentNullException(nameof(nextRoom));

            if (nextRoom is not DefaultRoom defaultRoom)
                throw new System.InvalidOperationException(
                    $"Room transition requires {nameof(DefaultRoom)}, but received {nextRoom.GetType().Name}.");

            if (defaultRoom.RoomData is not DefaultEnemiesRoomData roomData)
                throw new System.InvalidOperationException("Default room must contain DefaultEnemiesRoomData.");

            if (defaultRoom.EnterRoom == null)
                throw new System.InvalidOperationException("Default room enter door is not configured.");

            if (_characterProvider.CharacterFacade == null)
                throw new System.InvalidOperationException("Character is not available for room transition.");

            TransitAsync(defaultRoom, roomData).Forget();
        }

        private async UniTask TransitAsync(DefaultRoom defaultRoom, DefaultEnemiesRoomData roomData)
        {
            await _roomTransitionService.Play(() =>
            {
                _runtimeDataService.SetCurrentRoomData(roomData);

                var teleportPlayerTarget = defaultRoom.EnterRoom.transform;
                const int offset = 10;
                var characterPosition = teleportPlayerTarget.position + teleportPlayerTarget.forward * offset;

                _characterProvider.CharacterFacade.transform.position = characterPosition;

                _gameModeService.Get<RogueLikeStateMachine>().EnterState<RogueLikeRoomPrepareState>().Forget();
                return UniTask.CompletedTask;
            });
        }
    }

    public interface ITransitToRoomService
    {
        public void Transit(Room nextRoom);
    }
}
