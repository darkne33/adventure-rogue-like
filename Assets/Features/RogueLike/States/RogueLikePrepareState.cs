using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UI;
using UnityEngine;

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

        public RogueLikePrepareState(ICharacterFactory characterFactory,
            ISceneService<RogueLikeSceneProvider> sceneService, ICharacterProvider characterProvider,
            ILevelFactory levelFactory, IPanelService panelService,
            IRogueLikeRuntimeDataService rogueLikeRuntimeDataService, IAbilityChoiceProvider abilityChoiceProvider,
            ICameraService cameraService)
        {
            _characterFactory = characterFactory;
            _sceneService = sceneService;
            _characterProvider = characterProvider;
            _levelFactory = levelFactory;
            _panelService = panelService;
            _rogueLikeRuntimeDataService = rogueLikeRuntimeDataService;
            _abilityChoiceProvider = abilityChoiceProvider;
            _cameraService = cameraService;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            var panel =
                await _panelService.OpenPanelWithPresenter<CharacterPanel, CharacterPanelPresenter>(PanelName
                    .CharacterPanel);
            panel.Show().Forget();

            _abilityChoiceProvider.CreateAllAbilities();

            _characterProvider.CharacterFacade =
                await _characterFactory.CreatePlayer(_sceneService.GameSceneComponentsService.CharacterSpawnPoint, cts);

            _cameraService.MainCamera.Follow = _characterProvider.CharacterFacade.CameraPivot;

            _sceneService.GameSceneComponentsService.CurrentLevel =
                _levelFactory.CreateLevelView(_rogueLikeRuntimeDataService.CurrentIndexLevel,
                    _sceneService.GameSceneComponentsService.LevelSpawnPoint);

            var mainDoorTarget = _sceneService.GameSceneComponentsService.CurrentLevel.MainDoor.transform;
            const int offset = 10;
            var characterPosition = mainDoorTarget.position + mainDoorTarget.forward * offset;

            _characterProvider.CharacterFacade.transform.position = characterPosition;

            Log.Gameplay.Info("RogueLike Prepare State Completed");

            await StateMachine.EnterState<RogueLikeSpawnEnemyWaveState>();
        }
    }
}