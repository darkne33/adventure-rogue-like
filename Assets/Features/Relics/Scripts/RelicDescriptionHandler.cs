using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Features.PickupFeedback;
using UI;

namespace Features.Relics.Scripts
{
    public sealed class RelicDescriptionHandler : IDisposable
    {
        private readonly RelicEventBus _eventBus;
        private readonly IPanelService _panelService;
        private readonly IPauseService _pauseService;
        private readonly RelicManager _relicManager;
        private readonly ICharacterProvider _characterProvider;
        private readonly Queue<RelicDefinition> _pendingRelics = new();

        private RelicDescriptionPanel _panel;
        private RelicDefinition _currentRelic;
        private bool _isOpen;
        private bool _isClosing;

        public RelicDescriptionHandler(RelicEventBus eventBus, IPanelService panelService,
            IPauseService pauseService, RelicManager relicManager, ICharacterProvider characterProvider)
        {
            _eventBus = eventBus;
            _panelService = panelService;
            _pauseService = pauseService;
            _relicManager = relicManager;
            _characterProvider = characterProvider;

            _eventBus.RelicOffered += HandleRelicOffered;
        }

        public void Dispose()
        {
            _eventBus.RelicOffered -= HandleRelicOffered;

            if (_panel != null)
            {
                _panel.TakeRequested -= HandleTakeRequested;
                _panel.SkipRequested -= HandleSkipRequested;
            }

            if (_isOpen)
                _pauseService.CancelPause();
        }

        private void HandleRelicOffered(RelicDefinition relic)
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

            _currentRelic = _pendingRelics.Dequeue();
            _isOpen = true;

            _pauseService.HandlePause();
            panel.Show(_currentRelic);
        }

        private void HandleTakeRequested() =>
            ResolveCurrentRelic(true);

        private void HandleSkipRequested() =>
            ResolveCurrentRelic(false);

        private void ResolveCurrentRelic(bool takeRelic)
        {
            if (_isOpen == false || _isClosing)
                return;

            _isClosing = true;
            RelicDefinition collectedRelic = null;

            if (takeRelic)
            {
                if (_relicManager.AddRelic(_currentRelic) == false)
                {
                    _isClosing = false;
                    return;
                }

                collectedRelic = _currentRelic;
                _eventBus.PublishRelicCollected(_currentRelic);
            }

            _currentRelic = null;
            CloseCurrentRelic(collectedRelic).Forget();
        }

        private async UniTask CloseCurrentRelic(RelicDefinition collectedRelic)
        {
            await GetPanel().Hide();

            _pauseService.CancelPause();
            _isOpen = false;
            _isClosing = false;
            if (collectedRelic != null)
                _characterProvider?.CharacterFacade?.ShowPickupFeedback(
                    PickupFeedbackKind.Relic, collectedRelic.Icon);
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
            {
                _panel.TakeRequested += HandleTakeRequested;
                _panel.SkipRequested += HandleSkipRequested;
            }

            return _panel;
        }
    }
}
