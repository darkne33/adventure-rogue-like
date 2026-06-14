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
        var enemiesWaveObserver = _gameModeService.Get<RogueLikeStateMachine>().Resolve<EnemiesWaveObserver>();
        enemiesWaveObserver.RoomCompleted += UpdateRoomView;

        UpdateExpView(_characterLevelService.GetCurrentExp, _characterLevelService.GetMaxExp);
        UpdateRoomView();

        return UniTask.CompletedTask;
    }

    public override UniTask OnClosed()
    {
        _characterLevelService.OnUpdateAddExpView -= UpdateExpView;
        var enemiesWaveObserver = _gameModeService.Get<RogueLikeStateMachine>().Resolve<EnemiesWaveObserver>();
        enemiesWaveObserver.RoomCompleted -= UpdateRoomView;
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
        var enemiesWaveObserver = _gameModeService.Get<RogueLikeStateMachine>().Resolve<EnemiesWaveObserver>();
        Panel.RoomNumberText.text = $"ROOM {enemiesWaveObserver.CompletedRooms}";

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
