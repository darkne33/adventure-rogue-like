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

        public RogueLikePrepareState(ICharacterFactory characterFactory,
            ISceneService<RogueLikeSceneProvider> sceneService, ICharacterProvider characterProvider, ILevelFactory levelFactory, IPanelService panelService)
        {
            _characterFactory = characterFactory;
            _sceneService = sceneService;
            _characterProvider = characterProvider;
            _levelFactory = levelFactory;
            _panelService = panelService;
        }

        public override async UniTask Enter(CancellationToken cts)
        {
            var panel = await _panelService.OpenPanelWithPresenter<CharacterPanel, CharacterPanelPresenter>(PanelName.CharacterPanel);
            panel.Show().Forget();
            _characterProvider.CharacterFacade = await _characterFactory.CreatePlayer(_sceneService.GameSceneComponentsService.CharacterSpawnPoint, cts);

            _sceneService.GameSceneComponentsService.CurrentLevel = _levelFactory.CreateLevelView(1, _sceneService.GameSceneComponentsService.LevelSpawnPoint);
            
            Log.Gameplay.Info("RogueLike Prepare State Completed");
        }
    }
}