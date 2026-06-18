using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/EnemyWaveScalingConfiguration",
    fileName = "EnemyWaveScalingConfiguration", order = 0)]
public class EnemyWaveScalingConfiguration : ScriptableObject
{
    [SerializeField] private float _enemyCountMultiplier = 1f;
    [SerializeField] private int _additionalEnemiesPerLevel = 1;
    [SerializeField] private int _additionalEnemiesPerRoom = 1;
    [SerializeField] private int _additionalEnemiesPerWave = 2;
    [SerializeField] private int _maxEnemiesPerWave = 24;

    public int GetEnemyCount(int baseEnemyCount, int levelIndex, int roomIndex, int waveIndex)
    {
        if (baseEnemyCount <= 0)
            return 0;

        int scaledBaseCount = Mathf.CeilToInt(baseEnemyCount * Mathf.Max(1f, _enemyCountMultiplier));
        int levelBonus = Mathf.Max(0, levelIndex) * Mathf.Max(0, _additionalEnemiesPerLevel);
        int roomBonus = Mathf.Max(0, roomIndex) * Mathf.Max(0, _additionalEnemiesPerRoom);
        int waveBonus = Mathf.Max(0, waveIndex) * Mathf.Max(0, _additionalEnemiesPerWave);
        int enemyCount = scaledBaseCount + levelBonus + roomBonus + waveBonus;
        int maxEnemiesPerWave = _maxEnemiesPerWave <= 0 ? enemyCount : _maxEnemiesPerWave;

        return Mathf.Clamp(enemyCount, baseEnemyCount, maxEnemiesPerWave);
    }
}
