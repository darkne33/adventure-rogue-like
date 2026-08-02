using UnityEngine;

public enum EnemyRank
{
    Normal = 0,
    Elite = 1,
    Boss = 2
}

[CreateAssetMenu(menuName = "Configs/Enemies/EnemyConfiguration", fileName = "EnemyConfiguration", order = 0)]
public class EnemyConfiguration : ScriptableObject
{
    [field: Header("Identity Settings")]
    [field: SerializeField] public EnemyRank EnemyRank { get; private set; } = EnemyRank.Normal;

    [field: Header("Health Settings")]
    [field: Min(1)]
    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    [field: Header("Combat Settings")]
    
    [field: SerializeField] public int Damage { get; private set; }
    [field: SerializeField] public float DamageRange { get; private set; }
    [field: SerializeField] public float DamageCooldown { get; private set; }
    [field: Min(0f)]
    [field: SerializeField] public float InitialAttackCooldown { get; private set; }
    [field: Min(0f)]
    [field: SerializeField] public float AttackPreparationDuration { get; private set; } = 0.55f;
    [field: SerializeField] public EnemyDamageType EnemyDamageType { get; private set; }
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.RangeArea)]
    [field: Tooltip("Particle prefab spawned when a Range Area enemy detonates.")]
    [field: SerializeField] public GameObject ExplosionPrefab { get; private set; }
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.RangeArea)]
    [field: Tooltip("World-space offset applied to the spawned explosion prefab.")]
    [field: SerializeField] public Vector3 ExplosionOffset { get; private set; } =
        new(0f, 0.5f, 0f);
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.RangeArea)]
    [field: Tooltip("Radius in which the detonation damages the character.")]
    [field: Min(0f)]
    [field: SerializeField] public float AreaDamageRadius { get; private set; } = 2f;
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.RangeArea)]
    [field: Tooltip("Seconds before the spawned explosion prefab is destroyed.")]
    [field: Min(0.1f)]
    [field: SerializeField] public float ExplosionEffectLifetime { get; private set; } = 3f;
    [field: Tooltip("Minimum time the enemy remains stationary after releasing an attack. " +
                    "Attack-specific recovery can make the pause longer. " +
                    "Damage Cooldown starts after this pause.")]
    [field: Min(0f)]
    [field: SerializeField] public float MovementPauseAfterAttack { get; private set; } = 1.25f;
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.RangeBullet)]
    [field: SerializeField] public Features.Enemies.Scripts.EnemyBulletConfiguration BulletConfiguration { get; private set; }
    [field: SerializeField] public EnemyAnimationType EnemyAnimationType { get; private set; }
    [field: SerializeField] public EnemyMovementType EnemyMovementType { get; private set; }
    [field: SerializeField] public int Exp { get; private set; }

    [field: Header("Movement Settings")]
    [field: SerializeField] public float Speed { get; private set; }
    [field: SerializeField] public float RotationSpeed { get; private set; }
    [field: SerializeField] public float Acceleration { get; private set; }
    [field: NaughtyAttributes.ShowIf(nameof(UsesDistanceToStop))]
    [field: Min(0f)]
    [field: SerializeField] public float DistanceToStop { get; private set; }
    [field: NaughtyAttributes.ShowIf(nameof(EnemyMovementType), EnemyMovementType.Chase)]
    [field: Min(0f)]
    [field: SerializeField] public float CloseFollowDistance { get; private set; }
    [field: NaughtyAttributes.ShowIf(nameof(EnemyMovementType), EnemyMovementType.Chase)]
    [field: Min(0f)]
    [field: SerializeField] public float ResumeChaseDistance { get; private set; }
    [field: NaughtyAttributes.ShowIf(nameof(EnemyMovementType), EnemyMovementType.RangeChase)]
    [field: Tooltip("The enemy retreats when the character is closer than this distance.")]
    [field: Min(0f)]
    [field: SerializeField] public float RangeChaseMinimumDistance { get; private set; } = 6f;
    [field: NaughtyAttributes.ShowIf(nameof(EnemyMovementType), EnemyMovementType.RangeChase)]
    [field: Tooltip("The enemy approaches when the character is farther than this distance. " +
                    "Runtime movement is capped by Damage Range.")]
    [field: Min(0f)]
    [field: SerializeField] public float RangeChaseMaximumDistance { get; private set; } = 9f;
    [field: NaughtyAttributes.ShowIf(nameof(EnemyMovementType), EnemyMovementType.RangeChase)]
    [field: Tooltip("Angle in degrees used to select a new position after an attack.")]
    [field: Range(0f, 180f)]
    [field: SerializeField] public float RangeChaseRepositionAngle { get; private set; } = 60f;

    [field: Header("AI Settings")]
    [field: Tooltip("The enemy starts moving and attacking when the character enters this radius.")]
    [field: Min(0.1f)]
    [field: SerializeField] public float AggroRange { get; private set; } = 15f;
    [field: Tooltip("How long the enemy shows its alert before it can move or attack.")]
    [field: Min(0f)]
    [field: SerializeField] public float AggroReactionDuration { get; private set; } = 0.65f;

    private bool UsesDistanceToStop =>
        EnemyMovementType == EnemyMovementType.Chase ||
        EnemyMovementType == EnemyMovementType.AggressiveChase;
}
