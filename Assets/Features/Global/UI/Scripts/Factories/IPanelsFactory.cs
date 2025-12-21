using Cysharp.Threading.Tasks;

namespace UI
{
    public interface IPanelsFactory
    {
        UniTask<T> CreatePanel<T>(PanelName panelName) where T : PanelBase;
        void ClosePanel(PanelBase panel);
    }
}