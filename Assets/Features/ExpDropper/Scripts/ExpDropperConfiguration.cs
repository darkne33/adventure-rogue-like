using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Exp Dropper/ExpDropperConfiguration",
    fileName = "ExpDropperConfiguration", order = 0)]
public sealed class ExpDropperConfiguration : ScriptableObject
{
    [field: Header("Prefabs")]
    [field: SerializeField] public GameObject ExpRupeePrefab { get; private set; }
    [field: SerializeField] public GameObject PickupEffectPrefab { get; private set; }

    [field: Header("Rewards")]
    [field: Tooltip("Number of combat rooms using the early upgrade range, including the first room's guaranteed full level. The next room starts a new cycle using the later range.")]
    [field: SerializeField, Min(1)] public int EarlyCombatRoomCount { get; private set; } = 10;
    [field: Tooltip("Minimum random combat rooms per upgrade in both ranges after the first combat room, before XP bonuses.")]
    [field: SerializeField, Min(1)] public int MinCombatRoomsPerUpgrade { get; private set; } = 1;
    [field: Tooltip("Maximum random combat rooms per upgrade from the second combat room through EarlyCombatRoomCount, before XP bonuses.")]
    [field: SerializeField, Min(1)] public int EarlyMaxCombatRoomsPerUpgrade { get; private set; } = 2;
    [field: Tooltip("Maximum random combat rooms per upgrade after EarlyCombatRoomCount, before XP bonuses.")]
    [field: SerializeField, Min(1)] public int MaxCombatRoomsPerUpgrade { get; private set; } = 3;
    [field: SerializeField, Min(0f)] public float BossLevelProgress { get; private set; } = 1f;
    [field: SerializeField, Min(1f)] public float EliteExperienceMultiplier { get; private set; } = 2f;

    [field: Header("Spawn")]
    [field: SerializeField, Min(0f)] public float BurstHeight { get; private set; } = 0.85f;
    [field: SerializeField, Min(0f)] public float BurstScatterRadius { get; private set; } = 0.35f;
    [field: SerializeField, Min(0f)] public float BurstJumpPower { get; private set; } = 0.6f;
    [field: SerializeField, Min(0.01f)] public float BurstDuration { get; private set; } = 0.25f;
    [field: SerializeField, Min(0f)] public float AttractionStartDelay { get; private set; } = 0.05f;

    [field: Header("Flight")]
    [field: SerializeField, Min(0.01f)] public float CollectDistance { get; private set; } = 0.45f;
    [field: SerializeField, Min(0.01f)] public float AttractionSpeed { get; private set; } = 14f;
    [field: SerializeField, Min(0f)] public float CollectionTargetHeight { get; private set; } = 1f;
}
