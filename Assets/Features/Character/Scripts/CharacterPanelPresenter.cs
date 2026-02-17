using Cysharp.Threading.Tasks;
using DG.Tweening;
using UI;

public class CharacterPanelPresenter : PanelPresenter<CharacterPanel>
{
    private readonly ICharacterLevelService _characterLevelService;

    private Tween _tween;

    public CharacterPanelPresenter(ICharacterLevelService characterLevelService)
    {
        _characterLevelService = characterLevelService;
    }

    public override UniTask Initialize()
    {
        Panel.WaveAlertText.DOFade(0, 0);
        _characterLevelService.OnUpdateAddExpView += UpdateExpView;
        UpdateExpView(_characterLevelService.GetCurrentExp, _characterLevelService.GetMaxExp);
        
        return UniTask.CompletedTask;
    }

    public override UniTask OnClosed()
    {
        _characterLevelService.OnUpdateAddExpView -= UpdateExpView;
        return base.OnClosed();
    }

    private void UpdateExpView(int currentExp, int maxExp)
    {
        float value = (float)currentExp / maxExp;
        float duration = 0.3f;
        
        _tween?.Kill();

        _tween = Panel.ExpProgressBar.DOValue(value, duration);
    }
}