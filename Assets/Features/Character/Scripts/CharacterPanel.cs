using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPanel : PanelBase
{
    [field: SerializeField] public CharacterHealthView CharacterHealthView { get; private set; }
    [field: SerializeField] public CharacterGoldView CharacterGoldView { get; private set; }
    [field: SerializeField] public TMP_Text WaveAlertText { get; private set; }
    [field: SerializeField] public TMP_Text RoomNumberText { get; private set; }
    [field: SerializeField] public MinimapView MinimapView { get; private set; }
    [field: SerializeField] public Slider ExpProgressBar { get; private set; }
    [field: SerializeField] public UpgradeOfferPanel UpgradeOfferPanel { get; private set; }
}
