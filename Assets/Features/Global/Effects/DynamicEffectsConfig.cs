using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace Core.Services
{
    [CreateAssetMenu(menuName = "Data/Dynamic VFX collection")]
    public class DynamicEffectsConfig : ScriptableObject
    {
        [field: SerializeField, SerializedDictionary]
        public SerializedDictionary<EffectName, AddressableLoadContainerEffectPlayer> Effects { get; private set; }
    }
}