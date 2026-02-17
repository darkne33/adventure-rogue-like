using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/EnemyWavesConfiguration", fileName = "EnemyWavesConfiguration", order = 0)]
public class EnemyWavesConfiguration : ScriptableObject
{
    [field: SerializeField] public EnemyType[] EnemyTypes { get; private set; }
}