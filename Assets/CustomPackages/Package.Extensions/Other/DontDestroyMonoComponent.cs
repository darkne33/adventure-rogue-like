using UnityEngine;

namespace CustomPackages.Package.Extensions.Other
{
    public class DontDestroyMonoComponent : MonoBehaviour
    {
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}