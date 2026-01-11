using UI;
using UnityEngine;

public class CharacterPanel : PanelBase
{
    [field: SerializeField] public CharacterHealthView CharacterHealthView { get; private set; }
}