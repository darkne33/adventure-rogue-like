using Core;
using CustomPackages.Package.Extensions.Other;
using UnityEngine;

namespace UI
{
    [CreateAssetMenu(menuName = "Configs/UI/UIFactoryConfiguration")]
    public class UIFactoryConfig : ScriptableObject
    {
        [field: SerializeField] public AddressableLoadContainerGameObject UIRoot { get; private set; }

        public void Validate()
        {
            UIRoot.Validate(name);
        }
    }
}