using UnityEngine;

[CreateAssetMenu(menuName = "Create EnemyConfiguration", fileName = "EnemyConfiguration", order = 0)]
public class EnemyConfiguration : ScriptableObject
{
    [field: SerializeField] public float DistanceToStop { get; private set; }
    [field: SerializeField] public float Speed { get; private set; }
    [field: SerializeField] public float RotationSpeed { get; private set; }
    [field: SerializeField] public float Acceleration { get; private set; }
    [field: SerializeField] public float SmoothStopRange { get; private set; }
}