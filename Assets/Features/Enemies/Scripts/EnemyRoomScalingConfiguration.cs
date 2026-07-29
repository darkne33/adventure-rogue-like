using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/Enemy Room Scaling Configuration",
    fileName = "EnemyRoomScalingConfiguration", order = 0)]
public class EnemyRoomScalingConfiguration : ScriptableObject
{
    [SerializeField, Min(1f)] private float _enemyCountMultiplier = 1f;
    [SerializeField, Min(0)] private int _additionalEnemiesPerLevel = 1;
    [SerializeField, Min(0)] private int _additionalEnemiesPerRoom = 1;
    [SerializeField, Min(1)] private int _maxEnemiesPerRoom = 100;

    public int MaxEnemiesPerRoom => Mathf.Max(1, _maxEnemiesPerRoom);

    public int GetEnemyCount(int baseEnemyCount, int levelIndex, int completedCombatRooms)
    {
        if (baseEnemyCount <= 0)
            return 0;

        int scaledBaseCount = Mathf.CeilToInt(baseEnemyCount * Mathf.Max(1f, _enemyCountMultiplier));
        int levelBonus = Mathf.Max(0, levelIndex) * Mathf.Max(0, _additionalEnemiesPerLevel);
        int roomBonus = Mathf.Max(0, completedCombatRooms) * Mathf.Max(0, _additionalEnemiesPerRoom);
        int enemyCount = scaledBaseCount + levelBonus + roomBonus;

        return Mathf.Min(Mathf.Max(baseEnemyCount, enemyCount), MaxEnemiesPerRoom);
    }
}
