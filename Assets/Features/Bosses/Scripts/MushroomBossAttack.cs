using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Features.Bosses.Scripts
{
    public sealed class MushroomBossAttack : IBossAttack
    {
        private readonly BossAttackConfiguration _configuration;
        private readonly MushroomBossFacade _boss;

        public MushroomBossAttack(BossAttackConfiguration configuration, MushroomBossFacade boss)
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

            return _configuration switch
            {
                BossRadialAttackConfiguration radial =>
                    attacks.ExecuteRadialAttack(radial, cancellationToken, animateBoss, ownsAnimation),
                MushroomAttackConfiguration impact =>
                    attacks.ExecuteAttack(impact, cancellationToken, animateBoss, ownsAnimation),
                _ => throw new InvalidOperationException("Unsupported Mushroom attack configuration.")
            };
        }
    }
}
