using Cysharp.Threading.Tasks;
using UnityEngine.Scripting;

namespace UI
{
    [Preserve]
    public interface IPanelService
    {
        UniTask HidePanelWithAnimation<T>() where T : PanelBase;
        void HidePanelForce<T>() where T : PanelBase;
        UniTask AddOpenedPanel(PanelBase panel);
        UniTask HidePanelForce(PanelName componentPanelName);
        UniTask HidePanelWithAnimation(PanelName panelName);
        PanelBase GetPanel(PanelName panelName);

        UniTask<TPresenter> OpenPanelWithPresenter<TPanel, TPresenter>(PanelName panelName)
            where TPanel : PanelBase
            where TPresenter : IPanelPresenter<TPanel>;

        T GetPanelPresenter<T>(PanelName panelName) where T : IPanelPresenterLogic;

        UniTask<TPresenter> OpenPanelWithPresenterHidden<TPresenter>(PanelName panelName)
            where TPresenter : IPanelPresenterLogic;
    }
}