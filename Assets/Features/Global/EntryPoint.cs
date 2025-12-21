using UnityEngine;
using Zenject;

namespace Core
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private SceneContext _sceneContext;

        [Inject] private GameStateMachine _gameStateMachine;

        private void Start()
        {
            _gameStateMachine.EnterState<BootstrapState>();
        }
    }
}