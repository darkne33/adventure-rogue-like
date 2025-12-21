using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Infrastructure.SceneProvider;
using Zenject;
using Log = Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core.Log;

namespace Core
{
    public class LoadRogueLikeGameSceneState : State
    {
        [Inject] private ISceneLoader _sceneLoader;
        [Inject] private IGameModeService _gameModeService;
        [Inject] private DiContainer _container;

        public override async UniTask Enter(CancellationToken cts)
        {
            await _sceneLoader.LoadSceneFromAddressable(SceneNames.GameScene);
            _sceneLoader.UnloadBootstrapScene();

            var gameModeService = _container.Resolve<IGameModeService>();
            var gameSceneComponentsProvider = _sceneLoader
                .GetGameSceneComponentsProvider<GameSceneComponentsProvider>(SceneNames.GameScene);

            var sceneContext = gameSceneComponentsProvider
                .GetSceneContext();
            gameModeService.Add<RogueLikeStateMachine>(sceneContext.Container);

            await UniTask.WaitUntil(() => _sceneLoader.HasActiveScene(SceneNames.GameScene),
                cancellationToken: cts);

            gameSceneComponentsProvider.EnableScene();
            Log.Gameplay.Info("Done Load Game Scene State");

            await gameModeService.Get<RogueLikeStateMachine>().EnterState<RogueLikePrepareState>();
        }
    }
}