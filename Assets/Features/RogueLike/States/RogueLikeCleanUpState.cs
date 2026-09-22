using System.Threading;
using CustomPackages.Package.StateMachine.States;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Quests.Scripts;
using UI;

namespace Core
{
    public class RogueLikeCleanUpState : State
    {
        private readonly IPanelService _panelService;
        private readonly IPauseService _pauseService;
        private readonly QuestRunTracker _questRunTracker;

        public RogueLikeCleanUpState(IPanelService panelService, IPauseService pauseService,
            QuestRunTracker questRunTracker)
        {
            _panelService = panelService;
            _pauseService = pauseService;
            _questRunTracker = questRunTracker;
        }

        public override UniTask Enter(CancellationToken cts)
        {
            _questRunTracker.EndRun();
            _pauseService.HandlePause();

            CharacterPanel panel = _panelService
                .GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel)
                .Panel;

            panel.AnnouncementText.DOKill();
            panel.AnnouncementText.text = "RUN COMPLETE";
            panel.AnnouncementText.alpha = 1f;

            return UniTask.CompletedTask;
        }
    }
}
