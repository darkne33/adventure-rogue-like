using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

public class CharacterPanelPresenter : PanelPresenter<CharacterPanel>
{
    public CharacterPanel Panel { get; private set; }
    public override UniTask Initialize()
    {
        Debug.Log($"Initializing {Panel}");
        return UniTask.CompletedTask;
    }
}