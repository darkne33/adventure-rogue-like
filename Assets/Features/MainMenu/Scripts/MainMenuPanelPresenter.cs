using System.Threading;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine.EventSystems;

public sealed class MainMenuPanelPresenter : PanelPresenter<MainMenuPanel>
{
    private const int ShowAnimationMilliseconds = 300;

    private bool _playRequested;

    public override UniTask Initialize()
    {
        _playRequested = false;
        Panel.SetButtonsInteractable(false);
        Panel.PlayButton.onClick.AddListener(RequestPlay);

        EnableInputAfterShow(Panel.GetCancellationTokenOnDestroy()).Forget();

        return UniTask.CompletedTask;
    }

    public UniTask WaitForPlay(CancellationToken cancellationToken) =>
        UniTask.WaitUntil(() => _playRequested, cancellationToken: cancellationToken);

    public override UniTask OnClosed()
    {
        if (Panel != null)
            Panel.PlayButton.onClick.RemoveListener(RequestPlay);

        return base.OnClosed();
    }

    private void RequestPlay()
    {
        if (_playRequested)
            return;

        _playRequested = true;
        Panel.SetButtonsInteractable(false);
    }

    private async UniTask EnableInputAfterShow(CancellationToken cancellationToken)
    {
        await UniTask.Delay(ShowAnimationMilliseconds, ignoreTimeScale: true,
            cancellationToken: cancellationToken);

        if (Panel == null)
            return;

        Panel.SetButtonsInteractable(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(Panel.PlayButton.gameObject);
    }
}
