using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "Data/Settings/ClicksConfig", fileName = "ClicksConfig")]
    public class ClicksConfig : ScriptableObject
    {
        [field: SerializeField] public float PressTime { get; private set; }
        [field: SerializeField] public float PressTimeForClickDisable { get; private set; }
    }
}