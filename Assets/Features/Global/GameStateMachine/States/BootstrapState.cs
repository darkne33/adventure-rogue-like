using System.Threading;
using Core.Services;
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
        [Inject] private IEffectsService _effectsService;
        [Inject] private ILoadingScreenService _loadingScreenService;
        
        public override async UniTask Enter(CancellationToken cts)
        {
            Log.Gameplay.Info("Enter Bootstrap State");

            try
            {
                await _loadingScreenService.Show(cts);

                await _gameAddressableService.InitializeAddressables();
                await _cameraService.Initialize(cts);
                await _panelStorage.WarmUp(cts);
                await _uiFactory.Initialize(cts);
                await _effectsService.WarmUp(cts);

                Log.Gameplay.Info("Done Bootstrap State Initialization");
                // The next state keeps this screen visible until the main menu is ready.
                await StateMachine.EnterState<LoadRogueLikeGameSceneState>();
            }
            catch
            {
                await _loadingScreenService.Hide();
                throw;
            }
        }
    }
}
