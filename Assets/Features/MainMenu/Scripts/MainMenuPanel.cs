using UI;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuPanel : PanelBase
{
    [field: SerializeField] public Button PlayButton { get; private set; }
    [field: SerializeField] public Button QuestsButton { get; private set; }
    [field: SerializeField] public Button UnlocksButton { get; private set; }
    [field: SerializeField] public Button SettingsButton { get; private set; }

    public void SetButtonsInteractable(bool interactable)
    {
        PlayButton.interactable = interactable;
        QuestsButton.interactable = interactable;
        UnlocksButton.interactable = interactable;
        SettingsButton.interactable = interactable;
    }
}
