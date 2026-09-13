using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Features.Bosses.Scripts
{
    public sealed class BossCombatSystem : IDisposable
    {
        public BossState State { get; private set; } = BossState.Dormant;

        private readonly BossFacade _boss;
        private readonly BossAttackSystem _attackSystem;
        private readonly IBossAnimationSystem _animationSystem;
        private CancellationTokenSource _combatCancellation;

        public BossCombatSystem(BossFacade boss, BossAttackSystem attackSystem,
            IBossAnimationSystem animationSystem)
        {
            _boss = boss;
            _attackSystem = attackSystem;
            _animationSystem = animationSystem;
        }

        public void Start()
        {
            if (_boss == null || !_boss.isActiveAndEnabled || _boss.IsDead || _combatCancellation != null)
                return;

            _attackSystem.Initialize();
            State = BossState.Waiting;
            _combatCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _boss.GetCancellationTokenOnDestroy());
            RunCombat(_combatCancellation.Token).Forget();
        }

        public void Stop()
        {
            State = _boss != null && _boss.IsDead ? BossState.Dead : BossState.Dormant;
            CancellationTokenSource cancellation = _combatCancellation;
            _combatCancellation = null;
            cancellation?.Cancel();
            cancellation?.Dispose();
            _animationSystem.IdleAnimation();
        }

        public void Dispose() => Stop();

        internal void SetAttacking(bool attacking)
        {
            if (_boss != null && !_boss.IsDead && _boss.isActiveAndEnabled && State != BossState.Dormant)
                State = attacking ? BossState.Attacking : BossState.Waiting;
        }

        private async UniTask RunCombat(CancellationToken cancellationToken)
        {
            try
            {
                await _attackSystem.Tick(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }
    }
}
