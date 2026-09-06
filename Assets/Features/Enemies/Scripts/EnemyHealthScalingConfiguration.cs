using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/EnemyHealthScalingConfiguration",
    fileName = "EnemyHealthScalingConfiguration", order = 0)]
public class EnemyHealthScalingConfiguration : ScriptableObject
{
    [Tooltip("Health multipliers by combat depth, independent of the player's current build.")]
    [SerializeField] private float[] _healthByRoom =
        { 1f, 1.05f, 1.15f, 1.3f, 1.5f, 1.75f, 2f, 2.3f, 2.65f, 3f, 3.5f, 4f };
    [SerializeField, Min(1f)] private float _finalSpeedMultiplier = 1.2f;
    [SerializeField, Min(1f)] private float _finalDamageMultiplier = 1.6f;
    [SerializeField, Range(0.5f, 1f)] private float _finalAttackCooldownMultiplier = 0.85f;

    public int GetMaxHealth(int baseHealth, int roomIndex)
    {
        float multiplier = _healthByRoom == null || _healthByRoom.Length == 0
            ? 1f
            : Mathf.Max(1f, _healthByRoom[Mathf.Clamp(roomIndex, 0, _healthByRoom.Length - 1)]);
        return Mathf.Max(1, Mathf.CeilToInt(baseHealth * multiplier));
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
