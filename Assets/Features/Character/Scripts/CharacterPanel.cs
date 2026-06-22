using TMPro;
using Features.Relics.Scripts;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPanel : PanelBase
{
    [field: SerializeField] public CharacterHealthView CharacterHealthView { get; private set; }
    [field: SerializeField] public CharacterGoldView CharacterGoldView { get; private set; }
    [field: SerializeField] public CharacterExpView CharacterExpView { get; private set; }
    [field: SerializeField] public TMP_Text WaveAlertText { get; private set; }
    [field: SerializeField] public RoomTimerView RoomTimerView { get; private set; }
    [field: SerializeField] public TMP_Text RoomNumberText { get; private set; }
    [field: SerializeField] public TMP_Text PlayerGoldCurrencyText { get; private set; }
    [field: SerializeField] public TMP_Text PlayerSilverCurrencyText { get; private set; }
    [field: SerializeField] public TMP_Text PlayerKilledEnemiesText { get; private set; }
    [field: SerializeField] public TMP_Text GameTimerText { get; private set; }
    [field: SerializeField] public MinimapView MinimapView { get; private set; }
    [field: SerializeField] public Slider ExpProgressBar { get; private set; }
    [field: SerializeField] public UpgradeOfferPanel UpgradeOfferPanel { get; private set; }
    [field: SerializeField] public RelicInventoryView RelicInventoryViewPrefab { get; private set; }
}
