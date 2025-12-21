using UnityEditor;
using UnityEngine;

namespace CustomPackages.Package.Extensions
{
    public static class PrefabUtilityExtension
    {
        #if UNITY_EDITOR
        public static T InstantiatePrefab<T>(T prefab, Transform parent) where T : MonoBehaviour
        {
            var created = PrefabUtility.InstantiatePrefab(prefab, parent);
            T converted = created as T;
            return converted;
        }
        
        public static T InstantiatePrefab<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : MonoBehaviour
        {
            T created = InstantiatePrefab<T>(prefab, parent);

            created.transform.position = position;
            created.transform.rotation = rotation;

            return created;
        }
        
        public static T InstantiatePrefab<T>(T prefab, Vector3 position, Transform parent) where T : MonoBehaviour
        {
            T created = InstantiatePrefab<T>(prefab, parent);

            created.transform.position = position;

            return created;
        }
        
        public static GameObject InstantiatePrefab(GameObject prefab, Transform parent)
        {
            var created = PrefabUtility.InstantiatePrefab(prefab, parent);
            GameObject converted = created as GameObject;
            return converted;
        }
        
        public static GameObject InstantiatePrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject created = InstantiatePrefab(prefab, parent);

            created.transform.position = position;
            created.transform.rotation = rotation;

            return created;
        }
        
        public static GameObject InstantiatePrefab(GameObject prefab, Vector3 position, Transform parent)
        {
            GameObject created = InstantiatePrefab(prefab, parent);

            created.transform.position = position;

            return created;
        }
        #endif
    }
}