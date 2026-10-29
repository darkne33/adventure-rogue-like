using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Golem Slam Attack", fileName = "GolemSlamAttackConfiguration")]
    public sealed class GolemSlamAttackConfiguration : MushroomAttackConfiguration
    {
        public override IBossAttack CreateAttack(BossFacade boss, CharacterFacade character)
        {
            if (boss is not GolemBossFacade golem)
                throw new ArgumentException("Golem slam attacks require a GolemBossFacade.", nameof(boss));
            return new SlamAttack(this, golem);
        }

        private sealed class SlamAttack : IBossAttack
        {
            private readonly GolemSlamAttackConfiguration _configuration;
            private readonly GolemBossFacade _boss;

            public SlamAttack(GolemSlamAttackConfiguration configuration, GolemBossFacade boss)
            {
                _configuration = configuration;
                _boss = boss;
            }

            public UniTask Execute(CancellationToken cancellationToken, bool animateBoss = true,
                Func<bool> ownsAnimation = null)
            {
                if (_boss.AttackSystem is not GolemBossAttackSystem attacks)
                    throw new InvalidOperationException("Golem attacks require an initialized GolemBossAttackSystem.");
                return attacks.ExecuteAttack(_configuration, cancellationToken, animateBoss, ownsAnimation);
            }
        }
    }
}
