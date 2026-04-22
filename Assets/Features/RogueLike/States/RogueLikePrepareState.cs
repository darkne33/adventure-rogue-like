using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UI;

namespace Core
{
    public class RogueLikePrepareState : State
    {
        private readonly ICharacterFactory _characterFactory;
        private readonly ISceneService<RogueLikeSceneProvider> _sceneService;
        private readonly ICharacterProvider _characterProvider;
        private readonly ILevelFactory _levelFactory;
        private readonly IPanelService _panelService;
        private readonly IRogueLikeRuntimeDataService _rogueLikeRuntimeDataService;
        private readonly ICameraService _cameraService;
        private readonly IAbilityChoiceProvider _abilityChoiceProvider;
        private readonly IUpgradeOfferHandler _upgradeOfferHandler;

        public RogueLikePrepareState(ICharacterFactory characterFactory,
            ISceneService<RogueLikeSceneProvider> sceneService, ICharacterProvider characterProvider,
            ILevelFactory levelFactory, IPanelService panelService,
            IRogueLikeRuntimeDataService rogueLikeRuntimeDataService, IAbilityChoiceProvider abilityChoiceProvider,
            ICameraService cameraService, IUpgradeOfferHandler upgradeOfferHandler)
        {
            _characterFactory = characterFactory;
            _sceneService = sceneService;
            _characterProvider = characterProvider;
            _levelFactory = levelFactory;
            _panelService = panelService;
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _abilityChoiceProvider = abilityChoiceProvider;
            _cameraService = cameraService;
            _upgradeOfferHandler = upgradeOfferHandler;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            var panel =
                await _panelService.OpenPanelWithPresenter<CharacterPanel, CharacterPanelPresenter>(PanelName
                    .CharacterPanel);

            _abilityChoiceProvider.CreateAllAbilities();

            _sceneService.GameSceneComponentsService.CurrentLevel =
                _levelFactory.CreateLevelView(_rogueLikeRuntimeDataService.CurrentIndexLevel,
                    _sceneService.GameSceneComponentsService.LevelSpawnPoint);

            _rogueLikeRuntimeDataService.SetCurrentRoomData(_sceneService.GameSceneComponentsService.CurrentLevel
                .StartRoom.RoomData);

            _sceneService.GameSceneComponentsService.NavMeshSurface.RemoveData();
            _sceneService.GameSceneComponentsService.NavMeshSurface.BuildNavMesh();

            StartRoomData startRoomData = (StartRoomData)_rogueLikeRuntimeDataService.CurrentRoomData;

            foreach (var roomDoor in startRoomData.RoomDoors)
                roomDoor.Open();

            _characterProvider.CharacterFacade =
                await _characterFactory.CreatePlayer(startRoomData.StartPoint, cts);

            _cameraService.MainCamera.Follow = _characterProvider.CharacterFacade.CameraPivot;

            panel.Show().Forget();
            
            _upgradeOfferHandler.Handle();

            Log.Gameplay.Info("RogueLike Prepare State Completed");
        }
    }
}