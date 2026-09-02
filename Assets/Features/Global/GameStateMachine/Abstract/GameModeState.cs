using Core.Services;
using CustomPackages.Package.StateMachine;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Core
{
    public abstract class GameModeState : State
    {
        [Inject] private IScenesPreloader _scenesPreloader;

        protected abstract string SceneName { get; }
        protected abstract StateMachine GameModeStateMachine { get; }

        protected UniTask LoadScene() =>
            _scenesPreloader.WaitForSceneLoad(SceneName);

        protected async UniTask WaitForInitializaiton()
        {
            await UniTask.WaitWhile(() => GameModeStateMachine.Initialized == false,
                cancellationToken: StateMachine.CancellationToken.Token);
        }

        protected UniTask UnloadScene() =>
            _scenesPreloader.UnloadScene(SceneName);
    }
}