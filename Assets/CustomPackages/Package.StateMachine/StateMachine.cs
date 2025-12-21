using System;
using System.Collections.Generic;
using System.Threading;
using CustomPackages.Package.StateMachine.States;
using CustomPackages.Package.StateMachine.Transitions;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;

namespace CustomPackages.Package.StateMachine
{
    public class StateMachine : IDisposable
    {
        public Type ActiveState => _activeState?.GetType();
        public bool Initialized { get; private set; }
        public CancellationTokenSource CancellationToken { get; private set; } = new();
        public IReadOnlyDictionary<Type, IExitableState> States => _states;

        private readonly Dictionary<Type, IExitableState> _states = new Dictionary<Type, IExitableState>();
        private readonly Dictionary<Type, List<ITransition>> _transitions = new Dictionary<Type, List<ITransition>>();

        private IExitableState _activeState;
        private CancellationTokenSource _activeStateCts;

        public void ConfirmInitialization() =>
            Initialized = true;

        public void ResetInitialization() =>
            Initialized = false;

        public virtual void SetupNewToken()
        {
            CancellationToken = new CancellationTokenSource();
        }

        public void AddState(IExitableState state)
        {
            _states.Add(state.GetType(), state);

            if (state is IHavingStateMachine stateMachineHaving)
            {
                stateMachineHaving.SetStateMachine(this);
            }
        }

        public void RemoveState<T>() where T : IExitableState
        {
            if (_states.ContainsKey(typeof(T)))
            {
                _states.Remove(typeof(T));
            }
        }

        public async UniTask ReloadState()
        {
            if (_activeState != null)
            {
                await _activeState.Exit();
                await ((IState)_activeState).Enter();
            }
        }

        public bool TryGetState<T>(out T state) where T : IExitableState
        {
            Type stateType = typeof(T);
            bool hasState = _states.TryGetValue(stateType, out var foundState);

            state = (T)foundState;
            return hasState;
        }

        public bool HasState<T>() where T : State =>
            _states.ContainsKey(typeof(T));

        private bool TryGetState(Type type, out IExitableState state) =>
            _states.TryGetValue(type, out state);

        protected void AddTransition<TFrom, TTo>(Func<bool> permission)
            where TFrom : IExitableState
            where TTo : IExitableState
        {
            Type fromStateType = typeof(TFrom);
            Type toStateType = typeof(TTo);

            ITransition transition = new LambdaTransition(fromStateType, toStateType, permission);

            AddTransition(transition);
        }

        private void AddTransition(ITransition transition)
        {
            if (_transitions.TryGetValue(transition.FromState, out _) == false)
            {
                _transitions.Add(transition.FromState, new List<ITransition>());
            }

            _transitions[transition.FromState].Add(transition);
        }

        public UniTask EnterState<T>() where T : IState =>
            ChangeState(typeof(T));

        public UniTask EnterState(Type type) =>
            ChangeState(type);

        private async UniTask ChangeState(Type toStateType)
        {
            if (_activeState != null)
            {
                _activeStateCts?.Cancel();
                await _activeState.Exit(CancellationToken.Token);
            }

            if (TryGetState(toStateType, out var state))
            {
                _activeStateCts = new CancellationTokenSource();
                _activeState = state;
                await ((IState)state).Enter(_activeStateCts.Token);
            }
            else
            {
                Log.Gameplay.Error($"State with type ({toStateType} is missing in the state machine)");
            }
        }

        public void UpdateMachine()
        {
            UpdateTransitions();
            UpdateActiveState();
        }

        private void UpdateActiveState()
        {
            if (_activeState is IUpdatableState updatableState)
            {
                updatableState.Update();
            }
        }

        private void UpdateTransitions()
        {
            IExitableState permittedState = GetPermittedStateForTransition();

            if (permittedState == null) return;

            ChangeState(permittedState.GetType()).Forget();
        }

        private IExitableState GetPermittedStateForTransition()
        {
            if (_activeState == null || _transitions.Count == 0) return default;

            Type activeStateType = _activeState.GetType();

            if (_transitions.ContainsKey(activeStateType) == false) return default;

            List<ITransition> transitions = _transitions[activeStateType];

            foreach (var transition in transitions)
            {
                if (transition.GetPermission() == false) continue;

                TryGetState(transition.ToState, out var permittedState);
                return permittedState;
            }

            return null;
        }

        public void StopMachine()
        {
            _activeStateCts?.Cancel();
            CancellationToken.Cancel();
            _activeState?.Exit();
        }

        public void Dispose()
        {
            _activeStateCts?.Cancel();
            CancellationToken.Cancel();
        }
    }
}