using TMPro;
using Features.Relics.Scripts;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CharacterPanel : PanelBase
{
    [field: SerializeField] public CharacterHealthView CharacterHealthView { get; private set; }
    [field: SerializeField] public CharacterShieldView CharacterShieldView { get; private set; }
    [field: SerializeField] public CharacterGoldView CharacterGoldView { get; private set; }
    [field: SerializeField] public CharacterExpView CharacterExpView { get; private set; }
    [field: FormerlySerializedAs("<WaveAlertText>k__BackingField")]
    [field: SerializeField] public TMP_Text AnnouncementText { get; private set; }
    [field: SerializeField] public RoomTimerView RoomTimerView { get; private set; }
    [field: SerializeField] public TMP_Text RoomNumberText { get; private set; }
    [field: SerializeField] public TMP_Text PlayerGoldCurrencyText { get; private set; }
    [field: SerializeField] public TMP_Text PlayerSilverCurrencyText { get; private set; }
    [field: SerializeField] public TMP_Text PlayerKeysCurrencyText { get; private set; }
    [field: SerializeField] public TMP_Text PlayerKilledEnemiesText { get; private set; }
    [field: SerializeField] public TMP_Text GameTimerText { get; private set; }
    [field: SerializeField] public MinimapView MinimapView { get; private set; }
    [field: SerializeField] public Slider ExpProgressBar { get; private set; }
    [field: SerializeField] public ExpBarUpgradeMarquee ExpBarUpgradeMarquee { get; private set; }
    [field: SerializeField] public UpgradeOfferPanel UpgradeOfferPanel { get; private set; }
    [field: SerializeField] public CharacterBuildView CharacterBuildView { get; private set; }
    [field: SerializeField] public RelicInventoryView RelicInventoryView { get; private set; }
    [field: SerializeField] public RelicDescriptionPanel RelicDescriptionPanel { get; private set; }
    [field: SerializeField] public CharacterCrosshairView CrosshairView { get; private set; }
}
