using Cysharp.Threading.Tasks;

namespace UI
{
    public sealed class LoadingPanelPresenter : PanelPresenter<LoadingPanel>
    {
        public override UniTask Initialize() => UniTask.CompletedTask;
    }
}
