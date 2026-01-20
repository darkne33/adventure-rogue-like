using UnityEngine;

[CreateAssetMenu(menuName = "Create EnemyWavesConfiguration", fileName = "Configs/Enemies/EnemyWavesConfiguration", order = 0)]
public class EnemyWavesConfiguration : ScriptableObject
{
    [field: SerializeField] public EnemyType[] EnemyTypes { get; private set; }
}