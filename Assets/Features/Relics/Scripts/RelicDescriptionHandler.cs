using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UI;

namespace Features.Relics.Scripts
{
    public sealed class RelicDescriptionHandler : IDisposable
    {
        private readonly RelicEventBus _eventBus;
        private readonly IPanelService _panelService;
        private readonly IPauseService _pauseService;
        private readonly Queue<RelicDefinition> _pendingRelics = new();

        private RelicDescriptionPanel _panel;
        private bool _isOpen;
        private bool _isClosing;

        public RelicDescriptionHandler(RelicEventBus eventBus, IPanelService panelService,
            IPauseService pauseService)
        {
            _eventBus = eventBus;
            _panelService = panelService;
            _pauseService = pauseService;

            _eventBus.RelicCollected += HandleRelicCollected;
        }

        public void Dispose()
        {
            _eventBus.RelicCollected -= HandleRelicCollected;

            if (_panel != null)
                _panel.TakeRequested -= HandleTakeRequested;

            if (_isOpen)
                _pauseService.CancelPause();
        }

        private void HandleRelicCollected(RelicDefinition relic)
        {
            if (relic == null)
                return;

            _pendingRelics.Enqueue(relic);
            TryShowNextRelic();
        }

        private void TryShowNextRelic()
        {
            if (_isOpen || _isClosing || _pendingRelics.Count == 0)
                return;

            RelicDescriptionPanel panel = GetPanel();
            if (panel == null)
                return;

            RelicDefinition relic = _pendingRelics.Dequeue();
            _isOpen = true;

            _pauseService.HandlePause();
            panel.Show(relic);
        }

        private void HandleTakeRequested()
        {
            if (_isOpen == false || _isClosing)
                return;

            _isClosing = true;
            CloseCurrentRelic().Forget();
        }

        private async UniTask CloseCurrentRelic()
        {
            await GetPanel().Hide();

            _pauseService.CancelPause();
            _isOpen = false;
            _isClosing = false;
            TryShowNextRelic();
        }

        private RelicDescriptionPanel GetPanel()
        {
            if (_panel != null)
                return _panel;

            CharacterPanelPresenter presenter =
                _panelService.GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel);
            _panel = presenter?.Panel?.RelicDescriptionPanel;

            if (_panel != null)
                _panel.TakeRequested += HandleTakeRequested;

            return _panel;
        }
    }
}
