using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
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
        [Inject] private ILoadingScreenService _loadingScreenService;
        [Inject] private ICursorService _cursorService;
        [Inject] private DiContainer _container;

        public override UniTask Enter(CancellationToken cts) => ShowMainMenu(cts);

        private async UniTask<RogueLikeStateMachine> LoadGameScene(CancellationToken cancellationToken)
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

            await UniTask.WaitUntil(() => _sceneLoader.HasActiveScene(SceneNames.GameScene),
                cancellationToken: cancellationToken);

            gameSceneComponentsProvider.EnableScene();
            HeightFogRendererFeature.SetRenderingEnabled(true);
            Log.Gameplay.Info("Done Load Game Scene State");

            return rogueLikeStateMachine;
        }

        private async UniTask ShowMainMenu(CancellationToken cancellationToken)
        {
            _cursorService.ShowUiCursor();
            bool isPanelOpen = false;

            try
            {
                MainMenuPanelPresenter presenter = null;
                RogueLikeStateMachine rogueLikeStateMachine = null;
                await _loadingScreenService.Play(
                    async () =>
                    {
                        rogueLikeStateMachine = await LoadGameScene(cancellationToken);
                        presenter = await _panelService
                            .OpenPanelWithPresenterHidden<MainMenuPanelPresenter>(PanelName.MainMenuPanel);
                        isPanelOpen = true;
                        cancellationToken.ThrowIfCancellationRequested();
                    },
                    () => presenter.ForceShow(), cancellationToken);

                await presenter.WaitForPlay(cancellationToken);
                await _loadingScreenService.Play(
                    async () =>
                    {
                        await _panelService.HidePanelForce(PanelName.MainMenuPanel);
                        isPanelOpen = false;

                        HeightFogRendererFeature.SetRenderingEnabled(true);
                        await rogueLikeStateMachine.EnterState<RogueLikePrepareStatsState>();
                    },
                    _cursorService.ShowGameplayCursor, cancellationToken);
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
