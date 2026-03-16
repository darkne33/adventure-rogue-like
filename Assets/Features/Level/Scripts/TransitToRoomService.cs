using Core;
using Cysharp.Threading.Tasks;

namespace Features.Enemies.Scripts.Level.Scripts
{
    public class TransitToRoomService : ITransitToRoomService
    {
        private readonly IRogueLikeRuntimeDataService _runtimeDataService;
        private readonly ICharacterProvider _characterProvider;
        private readonly IGameModeService _gameModeService;

        public TransitToRoomService(IRogueLikeRuntimeDataService runtimeDataService,
            ICharacterProvider characterProvider, IGameModeService gameModeService)
        {
            _runtimeDataService = runtimeDataService;
            _characterProvider = characterProvider;
            _gameModeService = gameModeService;
        }

        public void Transit(Room nextRoom)
        {
            _runtimeDataService.SetCurrentRoomData(nextRoom.RoomData);
            var defaultRoom = (DefaultRoom)nextRoom;

            var teleportPlayerTarget = defaultRoom.EnterRoom.transform;
            const int offset = 10;
            var characterPosition = teleportPlayerTarget.position + teleportPlayerTarget.forward * offset;

            _characterProvider.CharacterFacade.transform.position = characterPosition;

            _gameModeService.Get<RogueLikeStateMachine>().EnterState<RogueLikeRoomPrepareState>().Forget();
        }
    }

    public interface ITransitToRoomService
    {
        public void Transit(Room nextRoom);
    }
}