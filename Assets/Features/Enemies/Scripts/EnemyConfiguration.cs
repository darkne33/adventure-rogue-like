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

    [field: Header("Damage Settings")]
    
    [field: SerializeField] public int Damage { get; private set; }
    [field: SerializeField] public float DamageRange { get; private set; }
    [field: SerializeField] public float DamageCooldown { get; private set; }
    [field: SerializeField] public EnemyDamageType EnemyDamageType { get; private set; }
    [field: SerializeField] public EnemyAnimationType EnemyAnimationType { get; private set; }
    [field: SerializeField] public EnemyMovementType EnemyMovementType { get; private set; }
    [field: SerializeField] public int Exp { get; private set; }

    [field: Header("Movement Settings")]
    
    [field: SerializeField] public float DistanceToStop { get; private set; }
    [field: SerializeField] public float Speed { get; private set; }
    [field: SerializeField] public float RotationSpeed { get; private set; }
    [field: SerializeField] public float Acceleration { get; private set; }
}
