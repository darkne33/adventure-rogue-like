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
        private readonly CharacterStats _characterStats;

        public RogueLikePrepareState(ICharacterFactory characterFactory,
            ISceneService<RogueLikeSceneProvider> sceneService, ICharacterProvider characterProvider,
            ILevelFactory levelFactory, IPanelService panelService,
            IRogueLikeRuntimeDataService rogueLikeRuntimeDataService, IAbilityChoiceProvider abilityChoiceProvider,
            ICameraService cameraService, IUpgradeOfferHandler upgradeOfferHandler, CharacterStats characterStats)
        {
            _characterFactory = characterFactory;
            _sceneService = sceneService;
            _characterProvider = characterProvider;
            _levelFactory = levelFactory;
            _panelService = panelService;
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _abilityChoiceProvider = abilityChoiceProvider;
            _cameraService = cameraService;
            _characterStats = characterStats;
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

            LevelView currentLevel = _sceneService.GameSceneComponentsService.CurrentLevel;
            if (currentLevel.StartRoom == null)
                throw new System.InvalidOperationException("The current level does not have a start room.");

            if (currentLevel.StartRoom.RoomData is not StartRoomData startRoomData)
                throw new System.InvalidOperationException("The level start room must contain StartRoomData.");

            if (startRoomData.StartPoint == null)
                throw new System.InvalidOperationException("The start room player spawn point is not configured.");

            if (startRoomData.RoomDoors == null)
                throw new System.InvalidOperationException("The start room doors are not configured.");

            _rogueLikeRuntimeDataService.SetCurrentRoomData(startRoomData);

            _sceneService.GameSceneComponentsService.NavMeshSurface.RemoveData();
            _sceneService.GameSceneComponentsService.NavMeshSurface.BuildNavMesh();

            foreach (var roomDoor in startRoomData.RoomDoors)
                roomDoor.Open();

            _characterProvider.CharacterFacade =
                await _characterFactory.CreatePlayer(startRoomData.StartPoint, cts);
            _characterProvider.CharacterFacade.Initialize();

            _characterProvider.CharacterFacade.CharacterAbilitySystem.AddAbility(
                _abilityChoiceProvider.GetAbility(AbilityName.RabbitBoomerang), _characterStats);

            _cameraService.MainCamera.Follow = _characterProvider.CharacterFacade.CameraPivot;

            panel.Show().Forget();

            Log.Gameplay.Info("RogueLike Prepare State Completed");
        }
    }
}
