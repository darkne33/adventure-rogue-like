using UnityEngine;

namespace Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Utilities
{
    public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<T>($"Logging/{typeof(T).Name}");
                }

                return _instance;
            }
        }
    }
}