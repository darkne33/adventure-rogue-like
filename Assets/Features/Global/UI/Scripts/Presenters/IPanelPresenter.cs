namespace UI
{
    public interface IPanelPresenter<out T> : IPanelPresenterView<T>, IPanelPresenterLogic where T : PanelBase
    {
    }
}