using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/EnemyTimedSpawnScalingConfiguration",
    fileName = "EnemyTimedSpawnScalingConfiguration", order = 0)]
public class EnemyTimedSpawnScalingConfiguration : ScriptableObject
{
    [SerializeField, Min(0f)] private float _additionalDurationPerCompletedCombatRoom = 2f;
    [SerializeField, Min(0f)] private float _maxDuration = 30f;

    public float GetDuration(float baseDuration, int completedCombatRooms)
    {
        float duration = Mathf.Max(0f, baseDuration);
        if (duration <= 0f)
            return 0f;

        float additionalDuration =
            Mathf.Max(0, completedCombatRooms) * Mathf.Max(0f, _additionalDurationPerCompletedCombatRoom);
        float scaledDuration = duration + additionalDuration;

        if (_maxDuration <= 0f)
            return scaledDuration;

        return Mathf.Min(scaledDuration, Mathf.Max(duration, _maxDuration));
    }
}
