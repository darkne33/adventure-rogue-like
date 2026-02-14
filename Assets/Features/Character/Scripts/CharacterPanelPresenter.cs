using Cysharp.Threading.Tasks;
using DG.Tweening;
using UI;
using UnityEngine;

public class CharacterPanelPresenter : PanelPresenter<CharacterPanel>
{
    public override UniTask Initialize()
    {
        Debug.Log($"Initializing {Panel}");
        Debug.Log(Panel);
        Panel.WaveAlertText.DOFade(0, 0);
        
        return UniTask.CompletedTask;
    }
}