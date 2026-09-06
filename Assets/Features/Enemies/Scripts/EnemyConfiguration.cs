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
    [field: Tooltip("Maximum random delay before the first attack after the aggro reaction. " +
                    "Zero disables the random delay.")]
    [field: Min(0f)]
    [field: SerializeField] public float RandomInitialAttackDelayMax { get; private set; }
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
    [field: Tooltip("Radius in which the detonation damages valid targets.")]
    [field: Min(0f)]
    [field: SerializeField] public float AreaDamageRadius { get; private set; } = 2f;
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.RangeArea)]
    [field: Tooltip("Whether the detonation also damages other enemies inside the area.")]
    [field: SerializeField] public bool DamagesEnemiesOnExplosion { get; private set; }
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.RangeArea)]
    [field: Tooltip("Whether this enemy also detonates when it dies.")]
    [field: SerializeField] public bool ExplodesOnDeath { get; private set; }
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

    [field: Header("Dash Settings")]
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.Dash)]
    [field: Tooltip("Distance travelled during the dash. Duration is distance divided by speed.")]
    [field: Min(0.01f)]
    [field: SerializeField] public float DashDistance { get; private set; } = 12f;
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.Dash)]
    [field: Tooltip("Dash speed in world units per second.")]
    [field: Min(0.01f)]
    [field: SerializeField] public float DashSpeed { get; private set; } = 28f;
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.Dash)]
    [field: Tooltip("How long the dash follows the target during Attack Preparation Duration. " +
                    "The direction is locked for the rest of the preparation.")]
    [field: Min(0f)]
    [field: SerializeField] public float DashTrackingDuration { get; private set; } = 0.35f;
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.Dash)]
    [field: Tooltip("Turning speed in degrees per second during dash preparation.")]
    [field: Min(0f)]
    [field: SerializeField] public float DashRotationSpeed { get; private set; } = 720f;
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.Dash)]
    [field: Tooltip("Sideways impulse perpendicular to the dash, applied once per hit. Zero disables it.")]
    [field: Min(0f)]
    [field: SerializeField] public float DashKnockbackForce { get; private set; } = 4f;
    [field: NaughtyAttributes.ShowIf(nameof(EnemyDamageType), EnemyDamageType.Dash)]
    [field: Tooltip("Additional upward impulse on a dash hit. Zero keeps the push horizontal.")]
    [field: Min(0f)]
    [field: SerializeField] public float DashKnockbackUpwardForce { get; private set; }

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

    public EnemyConfiguration CreateForRoom(EnemyHealthScalingConfiguration scaling,
        int roomIndex, bool isSplittingBomb)
    {
        EnemyConfiguration instance = Instantiate(this);
        instance.hideFlags = HideFlags.DontSave;
        // Splitting bombs share the normal bomb asset, but are elite encounters.
        if (isSplittingBomb)
        {
            instance.EnemyRank = EnemyRank.Elite;
            instance.MaxHealth = Mathf.CeilToInt(MaxHealth * 2.5f);
            instance.Speed = Speed * 0.8f;
            instance.Exp = 4;
        }

        instance.MaxHealth = scaling.GetMaxHealth(instance.MaxHealth, roomIndex);
        instance.Damage = scaling.GetDamage(instance.Damage, roomIndex);
        instance.Speed *= scaling.GetSpeedMultiplier(roomIndex);
        instance.DamageCooldown *= scaling.GetAttackCooldownMultiplier(roomIndex);
        return instance;
    }
}
