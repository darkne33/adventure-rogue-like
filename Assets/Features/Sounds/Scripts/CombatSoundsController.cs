using System;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using Zenject;

namespace Features.Sounds
{
    // Observe damage already applied by gameplay; audio never drives combat outcomes.
    public sealed class CombatSoundsController : IInitializable, IDisposable
    {
        private readonly ISoundsService _sounds;
        private readonly RelicEventBus _events;
        private readonly IEnemiesProvider _enemies;

        public CombatSoundsController(ISoundsService sounds, RelicEventBus events, IEnemiesProvider enemies)
        {
            _sounds = sounds;
            _events = events;
            _enemies = enemies;
        }

        public void Initialize()
        {
            _events.Hit += OnHit;
            _events.BossSpawned += OnBossSpawned;
            _events.RoomStarted += OnRoomStarted;
            _enemies.EnemyDefeated += OnEnemyDefeated;
        }

        public void Dispose()
        {
            _events.Hit -= OnHit;
            _events.BossSpawned -= OnBossSpawned;
            _events.RoomStarted -= OnRoomStarted;
            _enemies.EnemyDefeated -= OnEnemyDefeated;
            _sounds.StopGameplayLoops();
        }

        private void OnHit(RelicHitEvent hit)
        {
            if (hit.AppliedDamage <= 0 || hit.Target == null)
                return;

            SoundId sound = hit.SourceAbility?.Id switch
            {
                AbilityName.Punch => SoundId.PunchHit,
                // Boomerang contacts play their impact once in RabbitBoomerangFlight.
                AbilityName.RabbitBoomerang => SoundId.None,
                AbilityName.EarthRock => SoundId.StoneHit,
                AbilityName.Aura => SoundId.AuraHit,
                _ => hit.WeaponId == RelicBuildEffects.SacrificialDagger
                    ? SoundId.RelicDagger : GetHitSound(hit.Target.SpawnType)
            };
            _sounds.Play(sound);
            if (hit.IsCritical)
                _sounds.Play(SoundId.CriticalHit);
        }

        private static SoundId GetHitSound(EnemyType enemy) => enemy switch
        {
            EnemyType.Skeleton_Head => SoundId.BoneHit,
            EnemyType.Ghost => SoundId.GhostHit,
            EnemyType.WoodGuardBoss => SoundId.BossRoots,
            _ => SoundId.FleshHit
        };

        private void OnEnemyDefeated(CombatTarget target)
        {
            if (target == null)
                return;
            // Bomb detonation is sounded by its explosion, including death-triggered explosions.
            if (target is EnemyFacade enemy && enemy.Configuration.ExplodesOnDeath)
                return;
            _sounds.Play(target.Rank == EnemyRank.Boss ? SoundId.BossDeath : target.SpawnType switch
            {
                EnemyType.Skeleton_Head => SoundId.BoneDeath,
                EnemyType.Ghost => SoundId.GhostDeath,
                _ => SoundId.FleshDeath
            });
        }

        private void OnBossSpawned(RelicBossSpawnEvent spawn) => _sounds.Play(SoundId.BossWarning);
        private void OnRoomStarted(RelicRoomEvent room) => _sounds.StopGameplayLoops();
    }
}
