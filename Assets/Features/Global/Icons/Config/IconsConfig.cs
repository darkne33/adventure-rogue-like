using System;
using System.Collections.Generic;
using CustomPackages.Package.Extensions.Other;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "IconsConfig", menuName = "Data/IconsConfig")]
    public class IconsConfig : ScriptableObject
    {
        public List<IconSetup> Setups => _setups;
        
        [SerializeField] private List<IconSetup> _setups;

        [Serializable]
        public struct IconSetup
        {
            [SerializeReference, SubclassSelector] public RewardBase RewardType;
            public AddressableLoadContainerSprite Icon;
        }

        public void Validate()
        {
            foreach (var iconSetup in _setups)
            {
                iconSetup.Icon.Validate(name);
            }
        }
    }
}