using Zenject;

namespace UI
{
    public interface IPanelPresentersService
    {
        T Get<T>(PanelName panelName) where T : IPanelPresenterLogic;
        void Add(PanelName panelDataPanelName, DiContainer container);
        void Remove(PanelName panelName);
    }
}