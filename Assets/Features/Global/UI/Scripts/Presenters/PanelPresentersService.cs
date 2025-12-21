using System.Collections.Generic;
using Zenject;

namespace UI
{
    public class PanelPresentersService : IPanelPresentersService
    {
        [Inject] private IPanelPresentersFactory _factory;
        [Inject] private DiContainer _container;

        private readonly Dictionary<PanelName, IPanelPresenterLogic> _presenters = new();

        public void Add(PanelName panelName, DiContainer container) => 
            CreatePanelPresenter(panelName, container);

        public T Get<T>(PanelName panelName) where T : IPanelPresenterLogic
        {
            if (_presenters.TryGetValue(panelName, out var presenter))
                return (T)presenter;

            CreatePanelPresenter(panelName, _container);
            return (T)_presenters[panelName];
        }
        
        private void CreatePanelPresenter(PanelName panelName, DiContainer container) => 
            _presenters[panelName] = _factory.Create(panelName, container);
        public void Remove(PanelName panelName) => 
            _presenters.Remove(panelName);
    }
}