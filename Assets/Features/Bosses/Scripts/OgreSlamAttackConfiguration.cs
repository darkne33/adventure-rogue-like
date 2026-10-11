using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Ogre Slam Attack", fileName = "OgreSlamAttackConfiguration")]
    public sealed class OgreSlamAttackConfiguration : MushroomAttackConfiguration
    {
        [field: SerializeField, Min(0f)] public float Range { get; private set; } = 15f;
        [field: SerializeField, Min(0f)] public float Cooldown { get; private set; } = 4f;

        public override IBossAttack CreateAttack(BossFacade boss, CharacterFacade character)
        {
            if (boss is not OgreBossFacade ogre)
                throw new ArgumentException("Ogre slam attacks require an OgreBossFacade.", nameof(boss));
            return new SlamAttack(this, ogre);
        }

        private sealed class SlamAttack : IBossAttack
        {
            private readonly OgreSlamAttackConfiguration _configuration;
            private readonly OgreBossFacade _boss;

            public SlamAttack(OgreSlamAttackConfiguration configuration, OgreBossFacade boss)
            {
                _configuration = configuration;
                _boss = boss;
            }

            public UniTask Execute(CancellationToken cancellationToken, bool animateBoss = true,
                Func<bool> ownsAnimation = null)
            {
                if (_boss.AttackSystem is not OgreBossAttackSystem attacks)
                    throw new InvalidOperationException("Ogre attacks require an initialized OgreBossAttackSystem.");
                return attacks.ExecuteAttack(_configuration, cancellationToken, animateBoss, ownsAnimation);
            }
        }
    }
}
