using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Exp Dropper/ExpDropperConfiguration",
    fileName = "ExpDropperConfiguration", order = 0)]
public sealed class ExpDropperConfiguration : ScriptableObject
{
    [field: Header("Prefabs")]
    [field: SerializeField] public GameObject ExpRupeePrefab { get; private set; }
    [field: SerializeField] public GameObject PickupEffectPrefab { get; private set; }

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
