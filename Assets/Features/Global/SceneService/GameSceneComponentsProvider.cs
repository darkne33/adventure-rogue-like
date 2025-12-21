using UnityEngine;
using Zenject;

namespace Infrastructure.SceneProvider
{
    public abstract class GameSceneComponentsProvider : MonoBehaviour
    {
        public void EnableScene() =>
            gameObject.SetActive(true);

        public void DisableScene() =>
            gameObject.SetActive(false);

        public SceneContext GetSceneContext() => 
            GetComponent<SceneContext>();
    }
}