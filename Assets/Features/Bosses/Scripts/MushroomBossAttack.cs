using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Features.Bosses.Scripts
{
    public sealed class MushroomBossAttack : IBossAttack
    {
        private readonly MushroomAttackConfiguration _configuration;
        private readonly MushroomBossFacade _boss;

        public MushroomBossAttack(MushroomAttackConfiguration configuration, MushroomBossFacade boss)
        {
            _configuration = configuration;
            _boss = boss;
        }

        public UniTask Execute(CancellationToken cancellationToken, bool animateBoss = true,
            Func<bool> ownsAnimation = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_boss == null || _boss.IsDead)
                return UniTask.CompletedTask;
            if (_boss.AttackSystem is not MushroomBossAttackSystem attacks)
                throw new InvalidOperationException("Mushroom attacks require an initialized MushroomBossAttackSystem.");

            return attacks.ExecuteAttack(_configuration, cancellationToken, animateBoss, ownsAnimation);
        }
    }
}
