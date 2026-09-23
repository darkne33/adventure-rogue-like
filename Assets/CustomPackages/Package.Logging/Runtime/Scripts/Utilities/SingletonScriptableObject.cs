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
#if UNITY_EDITOR
                // Editor tooling reads the asset; runtime receives an Addressables-loaded instance.
                if (_instance == null && !Application.isPlaying)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
                    if (guids.Length > 0)
                        _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(
                            UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
#endif

                return _instance;
            }
        }

        public static void SetInstance(T instance) => _instance = instance;
    }
}
