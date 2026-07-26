using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/Enemy Room Configuration",
    fileName = "EnemyRoomConfiguration", order = 0)]
public class EnemyRoomConfiguration : ScriptableObject
{
    [field: SerializeField] public EnemyType[] EnemyTypes { get; private set; }
    [field: SerializeField, Min(0f)] public float TimedSpawnDuration { get; private set; } = 10f;
    [field: SerializeField, Min(0.1f)] public float AdditionalSpawnInterval { get; private set; } = 2f;
    [field: SerializeField, Min(0)] public int AdditionalEnemiesPerSpawn { get; private set; } = 1;

    public bool HasSpawnableEnemies
    {
        get
        {
            if (EnemyTypes == null)
                return false;

            for (int i = 0; i < EnemyTypes.Length; i++)
            {
                if (EnemyTypes[i] != EnemyType.None)
                    return true;
            }

            return false;
        }
    }
}
