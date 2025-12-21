using CustomPackages.Package.Extensions.Other;
using UnityEngine;

namespace Core.Services
{
    [CreateAssetMenu(menuName = "Data/Effects/FlyingEffectsConfig")]
    public class FlyingEffectsConfig : ScriptableObject
    {
        [field: SerializeField] public AddressableLoadContainerGameObject LevelPrefabContainer { get; private set; }
        [field: SerializeField] public AddressableLoadContainerGameObject FlyingLinearElementsPrefab { get; private set; }
        [field: SerializeField] public AddressableLoadContainerGameObject FlyingBoilingElementsPrefab { get; private set; }
        [field: SerializeField] public AddressableLoadContainerGameObject FlyingBoilingEnergyElementPrefab { get; private set; }

        public void Validate()
        {
            LevelPrefabContainer.Validate(name);
            FlyingLinearElementsPrefab.Validate(name);
            FlyingBoilingElementsPrefab.Validate(name);
            FlyingBoilingEnergyElementPrefab.Validate(name);
        }
    }
}