using Features.Enemies.Scripts;
using UnityEngine;

public abstract class CombatTarget : MonoBehaviour
{
    [field: SerializeField] public Transform TargetToShootDamage { get; private set; }

    public abstract HealthSystem HealthSystem { get; }
    public abstract DealDamageEffectSystem EffectsSystem { get; }
    public abstract Rigidbody Rigidbody { get; }
    public abstract Renderer[] MeshRenderers { get; }
    public abstract EnemyRank Rank { get; }
    public abstract float RelicTimeScale { get; }
    public bool IsDead => HealthSystem?.IsDead == true;
    public virtual EnemyType SpawnType { get; internal set; }

    public virtual Transform GetNextProjectileTarget() =>
        TargetToShootDamage != null ? TargetToShootDamage : transform;

    public abstract void SetPersistentRelicSlow(float multiplier);
    public abstract void ApplyRelicSlow(float multiplier, float duration);
    public abstract void ApplyRelicStun(float duration);
}
