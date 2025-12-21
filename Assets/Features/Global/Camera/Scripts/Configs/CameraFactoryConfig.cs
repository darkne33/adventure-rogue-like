using CustomPackages.Package.Extensions.Other;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "Configs/Core/CameraFactoryConfig")]
    public class CameraFactoryConfig : ScriptableObject
    {
        [field: SerializeField] public AddressableLoadContainerGameObject MainCamera { get; private set; }

        public void Validate()
        {
            MainCamera.Validate(name);
        }
    }
}