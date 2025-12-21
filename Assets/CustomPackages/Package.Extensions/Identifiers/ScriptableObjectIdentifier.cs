using CustomPackages.Package.Extensions.Other;
using NaughtyAttributes;
using UnityEngine;

namespace CustomPackages.Package.Extensions.Identifiers
{
    public abstract class ScriptableObjectIdentifier : ScriptableObject
    {
        [SerializeField, ReadOnly] private int id;

        public int Id
        {
            get
            {
                if (id == 0)
                    SetupIdentifier();
                return id;
            }
        }

        [Button]
        public void OnValidate() 
            => SetupIdentifier();

        private void SetupIdentifier() =>
            id = name.GenerateIndex();
    }
}