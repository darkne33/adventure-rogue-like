using Zenject;

namespace UI
{
    public interface IPanelPresentersFactory
    {
        IPanelPresenterLogic Create(PanelName panelName, DiContainer container);
    }
}