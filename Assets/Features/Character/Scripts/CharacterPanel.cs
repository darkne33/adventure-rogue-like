using TMPro;
using UI;
using UnityEngine;

public class CharacterPanel : PanelBase
{
    [field: SerializeField] public CharacterHealthView CharacterHealthView { get; private set; }
    [field: SerializeField] public TMP_Text WaveAlertText { get; private set; }
}