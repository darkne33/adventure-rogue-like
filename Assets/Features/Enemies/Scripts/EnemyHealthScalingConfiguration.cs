using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/EnemyHealthScalingConfiguration",
    fileName = "EnemyHealthScalingConfiguration", order = 0)]
public class EnemyHealthScalingConfiguration : ScriptableObject
{
    [SerializeField] private float _healthMultiplier = 1f;
    [SerializeField] private int _additionalHealthPerLevel = 6;
    [SerializeField] private int _additionalHealthPerRoom = 8;
    [SerializeField] private int _maxHealth = 250;

    public int GetMaxHealth(int baseHealth, int levelIndex, int roomIndex)
    {
        if (baseHealth <= 0)
            return 1;

        int scaledBaseHealth = Mathf.CeilToInt(baseHealth * Mathf.Max(1f, _healthMultiplier));
        int levelBonus = Mathf.Max(0, levelIndex) * Mathf.Max(0, _additionalHealthPerLevel);
        int roomBonus = Mathf.Max(0, roomIndex) * Mathf.Max(0, _additionalHealthPerRoom);
        int health = scaledBaseHealth + levelBonus + roomBonus;
        int maxHealth = _maxHealth <= 0 ? health : _maxHealth;

        return Mathf.Clamp(health, baseHealth, maxHealth);
    }
}
