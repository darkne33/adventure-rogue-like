using Cysharp.Threading.Tasks;

namespace UI
{
    public abstract class PanelPresenter<T> : IPanelPresenter<T> where T : PanelBase
    {
        public T Panel { get; private set; }

        public void Construct(PanelBase panelBase) =>
            Panel = (T)panelBase;

        public PanelBase GetPanel() =>
            Panel;

        public UniTask Show() =>
            Panel.GetComponent<PanelAnimationsMonoComponent>().Show();

        public UniTask Hide() =>
            Panel.GetComponent<PanelAnimationsMonoComponent>().Hide();

        public void ForceShow() =>
            Panel.GetComponent<PanelAnimationsMonoComponent>().ForceShow();

        public void ForceHide() =>
            Panel.GetComponent<PanelAnimationsMonoComponent>().ForceHide();

        public virtual void SetInput(bool state) =>
            Panel.GetComponent<PanelAnimationsMonoComponent>().SetInputState(state);

        public virtual UniTask OnClosed()
        {
            Panel = null;
            return UniTask.CompletedTask;
        }

        public abstract UniTask Initialize();
    }
}