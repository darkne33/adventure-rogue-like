using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    public class BossAttackSystem
    {
        private readonly BossFacade _boss;
        private readonly CharacterFacade _character;
        private readonly List<(BossAttackConfiguration Config, IBossAttack Attack)> _attacks = new();
        private BossAttackConfiguration[] _activeAttackPool;
        private float _cooldown;
        private int _nextAttack;

        public BossAttackSystem(BossFacade boss, CharacterFacade character)
        {
            _boss = boss;
            _character = character;
        }

        public virtual void Initialize()
        {
            _attacks.Clear();
            _activeAttackPool = null;
            _nextAttack = 0;
            _cooldown = Mathf.Max(0f, _boss.Config.InitialAttackDelay);
            _boss.AnimationSystem.IdleAnimation();
            UpdateAttackPool();
        }

        private void UpdateAttackPool()
        {
            HealthSystem health = _boss.HealthSystem;
            float healthPercentage = health.CurrentHealth / Mathf.Max(1f, health.MaxHealth) * 100f;
            BossAttackConfiguration[] attackPool = _boss.Config.GetAttacksForHealth(healthPercentage);
            if (ReferenceEquals(_activeAttackPool, attackPool))
                return;

            _activeAttackPool = attackPool;
            _attacks.Clear();
            _nextAttack = 0;
            if (attackPool == null)
                return;
            foreach (BossAttackConfiguration config in attackPool)
            {
                if (config != null)
                    _attacks.Add((config, config.CreateAttack(_boss, _character)));
            }
        }

        public virtual async UniTask Tick(CancellationToken cancellationToken)
        {
            while (_boss != null && !_boss.IsDead)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _boss.AnimationSystem.SetTimeScale(_boss.RelicTimeScale);
                if (_boss.CanAttack && _character != null && !_character.HealthSystem.IsDead &&
                    !_character.IsTransitionPaused)
                {
                    _cooldown -= Time.deltaTime * _boss.RelicTimeScale;
                    if (_cooldown <= 0f)
                    {
                        await Execute(cancellationToken);
                        _cooldown = Mathf.Max(0.01f, _boss.Config.AttackInterval);
                    }
                }
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public virtual async UniTask Execute(CancellationToken cancellationToken)
        {
            if (!_boss.CanAttack)
                return;
            UpdateAttackPool();
            for (int i = 0; i < _attacks.Count; i++)
            {
                var entry = _attacks[_nextAttack];
                _nextAttack = (_nextAttack + 1) % _attacks.Count;
                if (!entry.Config.IsEnabled)
                    continue;

                _boss.CombatSystem.SetAttacking(true);
                try
                {
                    await entry.Attack.Execute(cancellationToken);
                }
                finally
                {
                    if (_boss != null)
                    {
                        _boss.AnimationSystem.IdleAnimation();
                        _boss.CombatSystem.SetAttacking(false);
                    }
                }
                return;
            }
        }
    }
}
