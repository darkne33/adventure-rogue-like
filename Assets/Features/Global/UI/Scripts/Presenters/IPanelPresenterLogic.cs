using Cysharp.Threading.Tasks;

namespace UI
{
    public interface IPanelPresenterLogic
    {
        UniTask Initialize();
        UniTask OnClosed();
        UniTask Show();
        UniTask Hide();
        void ForceHide();
        void ForceShow();
        void SetInput(bool state);
        void Construct(PanelBase panelBase);
        PanelBase GetPanel();
    }
}