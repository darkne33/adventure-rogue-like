namespace UI
{
    public interface IPanelPresenterView<out T> where T : PanelBase
    {
        T Panel { get; }
    }
}