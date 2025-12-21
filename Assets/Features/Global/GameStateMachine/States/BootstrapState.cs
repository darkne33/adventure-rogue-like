using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UI;
using Zenject;

namespace Core
{
    public class BootstrapState : State
    {
        [Inject] private IGameAddressableService _gameAddressableService;
        [Inject] private ICameraService _cameraService;
        [Inject] private IPanelStorage _panelStorage;
        [Inject] private IUIFactory _uiFactory;
        
        public override async UniTask Enter(CancellationToken cts)
        {
            Log.Gameplay.Info("Enter Bootstrap State");

            await _gameAddressableService.InitializeAddressables();
            await _cameraService.Initialize(cts);
            await _panelStorage.WarmUp(cts);
            await _uiFactory.Initialize(cts);

            Log.Gameplay.Info("Done Bootstrap State Initialization");
            await StateMachine.EnterState<LoadRogueLikeGameSceneState>();
        }
    }
}