using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/Enemy Room Scaling Configuration",
    fileName = "EnemyRoomScalingConfiguration", order = 0)]
public class EnemyRoomScalingConfiguration : ScriptableObject
{
    [SerializeField] private float _enemyCountMultiplier = 1f;
    [SerializeField] private int _additionalEnemiesPerLevel = 1;
    [SerializeField] private int _additionalEnemiesPerRoom = 1;
    [SerializeField] private int _maxEnemiesPerRoom = 24;

    public int GetEnemyCount(int baseEnemyCount, int levelIndex, int roomIndex)
    {
        if (baseEnemyCount <= 0)
            return 0;

        int scaledBaseCount = Mathf.CeilToInt(baseEnemyCount * Mathf.Max(1f, _enemyCountMultiplier));
        int levelBonus = Mathf.Max(0, levelIndex) * Mathf.Max(0, _additionalEnemiesPerLevel);
        int roomBonus = Mathf.Max(0, roomIndex) * Mathf.Max(0, _additionalEnemiesPerRoom);
        int enemyCount = scaledBaseCount + levelBonus + roomBonus;
        int maxEnemiesPerRoom = _maxEnemiesPerRoom <= 0 ? enemyCount : _maxEnemiesPerRoom;

        return Mathf.Clamp(enemyCount, baseEnemyCount, maxEnemiesPerRoom);
    }
}
