using UnityEngine;
using UnityEngine.Serialization;

namespace Features.Bosses.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Bosses/Radial Attack", fileName = "RangeRadiusAttack")]
    public class BossRadialAttackConfiguration : BossAttackConfiguration
    {
        [Header("Projectile")]
        [Tooltip("Prepared BasicBigMissileRed prefab with MushroomRadialProjectile on its root.")]
        [SerializeField, FormerlySerializedAs("<ProjectilePrefab>k__BackingField")]
        private GameObject _projectilePrefab;
        [SerializeField, Min(3), FormerlySerializedAs("<ProjectileCount>k__BackingField")]
        private int _projectileCount = 24;
        [SerializeField, Min(0.01f), FormerlySerializedAs("<ProjectileSpeed>k__BackingField")]
        private float _projectileSpeed = 30f;
        [SerializeField, Min(0.01f), FormerlySerializedAs("<ProjectileLifetime>k__BackingField")]
        private float _projectileLifetime = 5f;
        [Tooltip("Damage of one projectile, in hearts.")]
        [SerializeField, Range(0f, 1f), FormerlySerializedAs("<Damage>k__BackingField")]
        private float _damage = 0.5f;

        [Header("Ring")]
        [Tooltip("Initial world-space ring radius. Mushroom split children apply their size multiplier.")]
        [SerializeField, Min(0f), FormerlySerializedAs("<SpawnRadius>k__BackingField")]
        private float _spawnRadius = 1.25f;

        [Header("Low-health burst")]
        [Tooltip("At or below this percentage of maximum health, release a series of rings.")]
        [SerializeField, Range(0f, 100f)] private float _burstHealthPercentage = 50f;
        [SerializeField, Min(1)] private int _burstRingCount = 3;
        [Tooltip("Delay between rings in boss time. The warning plays once before the series.")]
        [SerializeField, Min(0f)] private float _ringInterval = 0.2f;
        [Tooltip("Rotation in degrees added to each following ring in the series.")]
        [SerializeField, Range(-180f, 180f)] private float _ringAngleStep = 5f;

        [Header("Attack timing")]
        [Tooltip("Shared Ghost/Chan shake and white warning before firing, in boss time.")]
        [SerializeField, Min(0.01f), FormerlySerializedAs("_duration")]
        private float _warningDuration = 0.75f;
        [SerializeField, Min(0f), FormerlySerializedAs("<RecoveryDuration>k__BackingField")]
        private float _recoveryDuration = 0.35f;
        [Tooltip("Time between radial attack starts, in boss time.")]
        [SerializeField, Min(0.01f), FormerlySerializedAs("<Cooldown>k__BackingField")]
        private float _cooldown = 6f;

        public GameObject ProjectilePrefab => _projectilePrefab;
        public int ProjectileCount => _projectileCount;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public float Damage => _damage;
        public float SpawnRadius => _spawnRadius;
        public float BurstHealthPercentage => _burstHealthPercentage;
        public int BurstRingCount => _burstRingCount;
        public float RingInterval => _ringInterval;
        public float RingAngleStep => _ringAngleStep;
        public float WarningDuration => _warningDuration;
        public float RecoveryDuration => _recoveryDuration;
        public float Cooldown => _cooldown;

        public override IBossAttack CreateAttack(BossFacade boss, CharacterFacade character) =>
            new BossRadialAttack(this, boss, character);
    }
}
