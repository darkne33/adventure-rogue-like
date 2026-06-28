using System;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public sealed class RelicInventoryViewService : IDisposable
    {
        private readonly RelicManager _relicManager;

        private RelicInventoryView _view;

        public RelicInventoryViewService(RelicManager relicManager)
        {
            _relicManager = relicManager;
            _relicManager.Changed += Refresh;
        }

        public void Attach(CharacterPanel panel)
        {
            if (panel == null || _view != null)
                return;

            if (panel.RelicInventoryView == null)
            {
                Debug.LogWarning($"{nameof(CharacterPanel)} has no {nameof(CharacterPanel.RelicInventoryView)}.");
                return;
            }

            _view = panel.RelicInventoryView;
            Refresh();
        }

        public void Detach()
        {
            _view?.ClearSlots();

            _view = null;
        }

        public void Dispose()
        {
            _relicManager.Changed -= Refresh;
            Detach();
        }

        private void Refresh()
        {
            if (_view == null)
                return;

            _view.Refresh(_relicManager.ActiveRelics);
        }
    }
}
