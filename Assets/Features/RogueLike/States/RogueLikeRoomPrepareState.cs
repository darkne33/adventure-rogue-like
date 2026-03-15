using System;
using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;

namespace Core
{
    public class RogueLikeRoomPrepareState : State
    {
        private readonly ISceneService<RogueLikeSceneProvider> _sceneService;
        private readonly ICharacterProvider _characterProvider;
        private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;

        public RogueLikeRoomPrepareState(ISceneService<RogueLikeSceneProvider> sceneService, ICharacterProvider characterProvider, IRogueLikeRuntimeDataService rogueLikeRuntimeDataService)
        {
            _sceneService = sceneService;
            _characterProvider = characterProvider;
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            var mainDoorTarget = _sceneService.GameSceneComponentsService.CurrentLevel.MainDoor.transform;
            const int offset = 10;
            var characterPosition = mainDoorTarget.position + mainDoorTarget.forward * offset;

            _characterProvider.CharacterFacade.transform.position = characterPosition;

            var currentRoomData = _rogueLikeRuntimeDataService.CurrentRoomData;
            
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