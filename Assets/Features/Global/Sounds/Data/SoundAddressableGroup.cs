using System;
using Core.Services;
using CustomPackages.Package.Extensions.Other;

namespace Core.Sounds
{
    [Serializable]
    public struct SoundAddressableGroup
    {
        public SoundsGroupName SoundsGroupName;
        public AddressableLoadContainerScriptableObject Config;

        public void Validate(string name)
        {
            Config.Validate(name);
        }
    }
}