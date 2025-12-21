using Cysharp.Threading.Tasks;

namespace UI
{
    public interface IPanelAnimation
    {
        void Initialize();
        UniTask Show();
        void ForceShow();
        UniTask Hide();
        void ForceHide();
        void Cleanup();
    }
}