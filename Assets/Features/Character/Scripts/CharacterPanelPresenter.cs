using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using UI;
using UnityEngine;

public class CharacterPanelPresenter : PanelPresenter<CharacterPanel>
{
    private readonly ICharacterLevelService _characterLevelService;
    private readonly IGameModeService _gameModeService;

    private Tween _expTween;
    private Tween _roomTween;
    private EnemiesWaveObserver _enemiesWaveObserver;
    private MinimapController _minimapController;

    public CharacterPanelPresenter(ICharacterLevelService characterLevelService,
        IGameModeService gameModeService)
    {
        _characterLevelService = characterLevelService;
        _gameModeService = gameModeService;
    }

    public override UniTask Initialize()
    {
        Panel.WaveAlertText.DOFade(0, 0);
        _characterLevelService.OnUpdateAddExpView += UpdateExpView;
        RogueLikeStateMachine stateMachine = _gameModeService.Get<RogueLikeStateMachine>();
        _enemiesWaveObserver = stateMachine.Resolve<EnemiesWaveObserver>();
        _minimapController = stateMachine.Resolve<MinimapController>();

        _enemiesWaveObserver.RoomCompleted += UpdateRoomView;
        _minimapController.Attach(Panel.MinimapView);

        UpdateExpView(_characterLevelService.GetCurrentExp, _characterLevelService.GetMaxExp);
        UpdateRoomView();

        return UniTask.CompletedTask;
    }

    public override UniTask OnClosed()
    {
        _characterLevelService.OnUpdateAddExpView -= UpdateExpView;
        _enemiesWaveObserver.RoomCompleted -= UpdateRoomView;
        _minimapController.Detach(Panel.MinimapView);
        _expTween?.Kill();
        _roomTween?.Kill();
        return base.OnClosed();
    }

    private void UpdateExpView(int currentExp, int maxExp)
    {
        float value = (float)currentExp / maxExp;
        const float duration = 0.3f;

        _expTween?.Kill();
        _expTween = Panel.ExpProgressBar.DOValue(value, duration);
    }

    private void UpdateRoomView(DefaultEnemiesRoomData roomData = null)
    {
        Panel.RoomNumberText.text = $"ROOM {_enemiesWaveObserver.CompletedRooms}";

        if (roomData == null)
            return;

        RectTransform roomTextTransform = Panel.RoomNumberText.rectTransform;
        roomTextTransform.localScale = Vector3.one;
        _roomTween?.Kill();
        _roomTween = roomTextTransform
            .DOPunchScale(Vector3.one * 0.15f, 0.35f, 6, 0.5f)
            .SetUpdate(true);
    }
}
