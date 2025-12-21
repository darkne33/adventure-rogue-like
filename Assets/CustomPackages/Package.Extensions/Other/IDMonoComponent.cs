using NaughtyAttributes;
using UnityEngine;

namespace CustomPackages.Package.Extensions.Other
{
    public class IDMonoComponent : MonoBehaviour
    {
        [SerializeField, ReadOnly] private int id;

        public int Id
        {
            get
            {
                if (id == 0)
                    Validate();
                return id;
            }
        }

        public void Validate() => 
            id = gameObject.name.GenerateIndex();

        private void OnValidate() =>
            Validate();
    }
}