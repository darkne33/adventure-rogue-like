using System.Collections.Generic;
using System.Linq;
using CustomPackages.Package.StateMachine;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UnityEngine;
using Zenject;

namespace Core
{
    public class GameModeService : IGameModeService
    {
        private readonly List<ZenjectStateMachine> _stateMachines = new();

        public void Add<T>(DiContainer diContainer) where T : ZenjectStateMachine
        {
            Remove<T>();

            var stateMachine = diContainer.Instantiate<T>();
            diContainer.Bind<ZenjectStateMachine>().FromInstance(stateMachine).AsSingle();
            Log.Unity.Info($"Added state machine: {stateMachine}");
            _stateMachines.Add(stateMachine);
        }

        public T Get<T>() where T : ZenjectStateMachine
        {
            var stateMachine = (T)_stateMachines.FirstOrDefault(x => x.GetType() == typeof(T));
            return stateMachine;
        }

        public bool Remove<T>() where T : ZenjectStateMachine
        {
            ZenjectStateMachine stateMachine =
                _stateMachines.FirstOrDefault(x => x.GetType() == typeof(T));
            if (stateMachine == null)
                return false;

            stateMachine.Dispose();
            _stateMachines.Remove(stateMachine);
            return true;
        }
    }
}
