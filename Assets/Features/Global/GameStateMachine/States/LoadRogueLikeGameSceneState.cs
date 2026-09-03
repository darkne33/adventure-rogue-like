using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts.Level.Scripts;
using Infrastructure.SceneProvider;
using LittleRush.Rendering;
using UI;
using Zenject;
using Log = Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core.Log;

namespace Core
{
    public class LoadRogueLikeGameSceneState : State
    {
        [Inject] private ISceneLoader _sceneLoader;
        [Inject] private IGameModeService _gameModeService;
        [Inject] private IPanelService _panelService;
        [Inject] private ICursorService _cursorService;
        [Inject] private DiContainer _container;

        public override async UniTask Enter(CancellationToken cts)
        {
            HeightFogRendererFeature.SetRenderingEnabled(false);

            if (_sceneLoader.HasActiveScene(SceneNames.GameScene))
                await _sceneLoader.ReloadSceneFromAddressable(SceneNames.GameScene);
            else
                await _sceneLoader.LoadSceneFromAddressable(SceneNames.GameScene);

            _sceneLoader.UnloadBootstrapScene();

            var gameModeService = _container.Resolve<IGameModeService>();
            var gameSceneComponentsProvider = _sceneLoader
                .GetGameSceneComponentsProvider<GameSceneComponentsProvider>(SceneNames.GameScene);

            var sceneContext = gameSceneComponentsProvider
                .GetSceneContext();
            gameModeService.Add<RogueLikeStateMachine>(sceneContext.Container);
            var rogueLikeStateMachine = gameModeService.Get<RogueLikeStateMachine>();
            var transitionService = sceneContext.Container.Resolve<IRoomTransitionService>();

            await UniTask.WaitUntil(() => _sceneLoader.HasActiveScene(SceneNames.GameScene),
                cancellationToken: cts);

            gameSceneComponentsProvider.EnableScene();
            Log.Gameplay.Info("Done Load Game Scene State");

            await ShowMainMenu(transitionService, rogueLikeStateMachine, cts);
        }

        private async UniTask ShowMainMenu(IRoomTransitionService transitionService,
            RogueLikeStateMachine rogueLikeStateMachine, CancellationToken cancellationToken)
        {
            _cursorService.ShowUiCursor();
            bool isPanelOpen = false;

            try
            {
                var presenter = await _panelService
                    .OpenPanelWithPresenter<MainMenuPanel, MainMenuPanelPresenter>(PanelName.MainMenuPanel);
                isPanelOpen = true;

                await presenter.WaitForPlay(cancellationToken);
                await transitionService.PlayLoading(
                    async () =>
                    {
                        await _panelService.HidePanelForce(PanelName.MainMenuPanel);
                        isPanelOpen = false;

                        HeightFogRendererFeature.SetRenderingEnabled(true);
                        await rogueLikeStateMachine.EnterState<RogueLikePrepareStatsState>();
                    },
                    _cursorService.ShowGameplayCursor);
            }
            finally
            {
                try
                {
                    if (isPanelOpen)
                        await _panelService.HidePanelForce(PanelName.MainMenuPanel);
                }
                finally
                {
                    _cursorService.ShowGameplayCursor();
                }
            }
        }
    }
}
