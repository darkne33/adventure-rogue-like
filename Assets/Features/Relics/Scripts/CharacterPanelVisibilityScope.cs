using System;
using UI;
using UnityEngine;

namespace Features.Relics.Scripts
{
    internal sealed class CharacterPanelVisibilityScope : IDisposable
    {
        private readonly CharacterPanelPresenter _presenter;
        private readonly CharacterPanel _panel;
        private readonly CanvasGroup _canvasGroup;
        private readonly float _alpha;
        private readonly bool _interactable;
        private readonly bool _blocksRaycasts;
        private readonly bool _hasCapturedState;

        private bool _isDisposed;

        private CharacterPanelVisibilityScope(IPanelService panelService)
        {
            _presenter = panelService?.GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel);
            _panel = _presenter?.Panel;
            _canvasGroup = _panel != null ? _panel.GetComponent<CanvasGroup>() : null;

            if (_canvasGroup == null)
                return;

            _alpha = _canvasGroup.alpha;
            _interactable = _canvasGroup.interactable;
            _blocksRaycasts = _canvasGroup.blocksRaycasts;
            _hasCapturedState = true;
            _presenter.ForceHide();
        }

        public static CharacterPanelVisibilityScope Hide(IPanelService panelService) =>
            new(panelService);

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            bool isSamePanel = _presenter != null && ReferenceEquals(_presenter.Panel, _panel);
            bool isStillHiddenBySequence = _canvasGroup != null &&
                                           Mathf.Approximately(_canvasGroup.alpha, 0f) &&
                                           _canvasGroup.interactable == false &&
                                           _canvasGroup.blocksRaycasts == false;

            if (_hasCapturedState == false || _panel == null || isSamePanel == false ||
                isStillHiddenBySequence == false)
                return;

            _canvasGroup.alpha = _alpha;
            _canvasGroup.interactable = _interactable;
            _canvasGroup.blocksRaycasts = _blocksRaycasts;
        }
    }
}
