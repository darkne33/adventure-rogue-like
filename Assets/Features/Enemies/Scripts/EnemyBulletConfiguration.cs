using UnityEngine;

namespace Features.Enemies.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Enemies/Enemy Bullet Configuration",
        fileName = "EnemyBulletConfiguration", order = 1)]
    public sealed class EnemyBulletConfiguration : ScriptableObject
    {
        [field: Header("Projectile")]
        [field: SerializeField] public EnemyBullet ProjectilePrefab { get; private set; }
        [field: SerializeField, Min(0.1f)] public float Speed { get; private set; } = 12f;
        [field: SerializeField, Min(0.1f)] public float Lifetime { get; private set; } = 4f;

        [field: Header("Attack Timing")]
        [field: SerializeField, Min(0f)] public float RecoveryDuration { get; private set; } = 0.25f;

        [field: Header("Aiming")]
        [field: SerializeField] public Vector3 TargetOffset { get; private set; }
    }
}
