using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UnityEngine;
using UnityEngine.Scripting;
using Zenject;

namespace UI
{
    [Preserve]
    public sealed class PanelService : IPanelService
    {
        [Inject] private IPanelsFactory _panelsFactory;
        [Inject] private IPanelPresentersService _panelPresentersService;
        [Inject] private IPanelsProvider _panelsProvider;

        public async UniTask<TPresenter> OpenPanelWithPresenter<TPanel, TPresenter>(PanelName panelName)
            where TPanel : PanelBase where TPresenter : IPanelPresenter<TPanel>
        {
            var presenter = GetPanelPresenter<TPresenter>(panelName);
            if (presenter.Panel != null)
            {
                Log.Gameplay.Error("You are opening panel that already exist");
                await HidePanelForce(panelName);
            }

            var panel = await _panelsFactory.CreatePanel<TPanel>(panelName);
            
            Debug.Log(panel);
            
            presenter.Construct(panel);
            presenter.ForceHide();
            await AddOpenedPanel(panel);
            await presenter.Initialize();
            presenter.Show().Forget();
            return presenter;
        }

        public async UniTask<TPresenter> OpenPanelWithPresenterHidden<TPresenter>(PanelName panelName)
            where TPresenter : IPanelPresenterLogic
        {
            var presenter = GetPanelPresenter<TPresenter>(panelName);
            var panel = await _panelsFactory.CreatePanel<PanelBase>(panelName);
            presenter.Construct(panel);
            presenter.ForceHide();
            await AddOpenedPanel(panel);

            await presenter.Initialize();
            return presenter;
        }

        public T GetPanelPresenter<T>(PanelName panelName) where T : IPanelPresenterLogic =>
            _panelPresentersService.Get<T>(panelName);

        public PanelBase GetPanel(PanelName panelName) => 
            GetPanelPresenter<IPanelPresenterLogic>(panelName).GetPanel();

        public void HidePanelForce<T>() where T : PanelBase
        {
            var panel = _panelsProvider.GetOpenedPanel<T>();

            if (panel == null) return;

            DestroyPanel(panel).Forget();
        }

        public async UniTask HidePanelWithAnimation<T>() where T : PanelBase
        {
            var panel = _panelsProvider.GetOpenedPanel<T>();

            if (panel == null) return;
            await panel.GetComponent<PanelAnimationsMonoComponent>().Hide();
            await DestroyPanel(panel);
        }

        public async UniTask HidePanelWithAnimation(PanelName panelName)
        {
            var panel = GetPanel(panelName);

            if (panel == null) return;
            await panel.GetComponent<PanelAnimationsMonoComponent>().Hide();
            await DestroyPanel(panel);
        }

        public UniTask HidePanelForce(PanelName panelName)
        {
            var panel = GetPanel(panelName);

            if (panel == null) return UniTask.CompletedTask;

            return DestroyPanel(panel);
        }

        public UniTask AddOpenedPanel(PanelBase panel) => 
            _panelsProvider.AddOpenedPanel(panel);

        private async UniTask DestroyPanel(PanelBase panel)
        {
            var presenter = GetPanelPresenter<IPanelPresenterLogic>(panel.PanelName);
            await presenter.OnClosed();
            _panelsFactory.ClosePanel(panel);
            await _panelsProvider.RemoveOpenedPanel(panel);
        }
    }
}