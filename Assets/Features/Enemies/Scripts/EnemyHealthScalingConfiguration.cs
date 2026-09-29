using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/EnemyHealthScalingConfiguration",
    fileName = "EnemyHealthScalingConfiguration", order = 0)]
public class EnemyHealthScalingConfiguration : ScriptableObject
{
    [Tooltip("Health multipliers by first combat-room visit order in the run, independent of the player's current build.")]
    [SerializeField] private float[] _healthByRoom =
        { 2.5f, 2.75f, 3f, 3.5f, 4f, 4.5f, 5f, 5.75f, 6.5f, 7.5f, 8.75f, 10f };
    [Tooltip("Scales only health growth above the first combat room, including additional floor growth. Starting health stays unchanged.")]
    [SerializeField, Range(0f, 1f)] private float _healthGrowthMultiplier = 0.85f;
    [SerializeField, Min(1f)] private float _finalSpeedMultiplier = 1.2f;
    [SerializeField, Min(1f)] private float _finalDamageMultiplier = 1.6f;
    [SerializeField, Range(0.5f, 1f)] private float _finalAttackCooldownMultiplier = 0.85f;

    public int GetMaxHealth(int baseHealth, int roomIndex,
        ProceduralLevelSettings proceduralSettings = null, int generatedLevelNumber = 0)
    {
        float startingMultiplier = _healthByRoom == null || _healthByRoom.Length == 0
            ? 1f
            : Mathf.Max(1f, _healthByRoom[0]);
        float multiplier = _healthByRoom == null || _healthByRoom.Length == 0
            ? 1f
            : Mathf.Max(1f, _healthByRoom[Mathf.Clamp(roomIndex, 0, _healthByRoom.Length - 1)]);
        int startingHealth = Mathf.Max(1, Mathf.CeilToInt(baseHealth * startingMultiplier));
        int scaledHealth = Mathf.Max(1, Mathf.CeilToInt(baseHealth * multiplier));
        if (proceduralSettings != null && generatedLevelNumber > 0)
            scaledHealth = proceduralSettings.ScaleHealth(scaledHealth, generatedLevelNumber);

        // Apply the reduction once after both growth sources, preserving the initial HP.
        return Mathf.Max(1, Mathf.CeilToInt(startingHealth +
            (scaledHealth - startingHealth) * Mathf.Clamp01(_healthGrowthMultiplier)));
    }

    public float GetSpeedMultiplier(int roomIndex) =>
        Mathf.Lerp(1f, _finalSpeedMultiplier, GetProgress(roomIndex));

    public int GetDamage(int baseDamage, int roomIndex) =>
        Mathf.Max(baseDamage, Mathf.RoundToInt(baseDamage *
            Mathf.Lerp(1f, _finalDamageMultiplier, GetProgress(roomIndex))));

    public float GetAttackCooldownMultiplier(int roomIndex) =>
        Mathf.Lerp(1f, _finalAttackCooldownMultiplier, GetProgress(roomIndex));

    private float GetProgress(int roomIndex) =>
        Mathf.Clamp01((float)roomIndex / Mathf.Max(1, (_healthByRoom?.Length ?? 12) - 1));
}
