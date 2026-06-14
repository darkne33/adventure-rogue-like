using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UI;

namespace Core
{
    public class RogueLikeCleanUpState : State
    {
        private readonly IPanelService _panelService;
        private readonly IPauseService _pauseService;

        public RogueLikeCleanUpState(IPanelService panelService, IPauseService pauseService)
        {
            _panelService = panelService;
            _pauseService = pauseService;
        }

        public override UniTask Enter(CancellationToken cts)
        {
            _pauseService.HandlePause();

            CharacterPanel panel = _panelService
                .GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel)
                .Panel;

            panel.WaveAlertText.DOKill();
            panel.WaveAlertText.text = "RUN COMPLETE";
            panel.WaveAlertText.alpha = 1f;

            return UniTask.CompletedTask;
        }
    }
}
