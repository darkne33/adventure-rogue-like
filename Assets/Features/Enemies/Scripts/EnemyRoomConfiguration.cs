using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/Enemy Room Configuration",
    fileName = "EnemyRoomConfiguration", order = 0)]
public class EnemyRoomConfiguration : ScriptableObject
{
    [field: SerializeField] public EnemyType[] EnemyTypes { get; private set; }

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

[System.Serializable]
public sealed class EnemyRoomSettings
{
    [field: SerializeField]
    [field: Tooltip("Enemy types that can be spawned in this room.")]
    public EnemyType[] EnemyTypes { get; private set; } = System.Array.Empty<EnemyType>();

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

    public EnemyRoomSettings()
    {
    }

    public EnemyRoomSettings(EnemyRoomConfiguration configuration)
    {
        if (configuration == null)
            throw new System.ArgumentNullException(nameof(configuration));

        EnemyTypes = configuration.EnemyTypes != null
            ? (EnemyType[])configuration.EnemyTypes.Clone()
            : System.Array.Empty<EnemyType>();
    }
}
