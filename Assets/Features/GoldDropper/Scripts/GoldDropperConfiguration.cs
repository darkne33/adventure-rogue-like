using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Gold Dropper/GoldDropperConfiguration",
    fileName = "GoldDropperConfiguration", order = 0)]
public sealed class GoldDropperConfiguration : ScriptableObject
{
    [field: Header("Reward")]
    [field: SerializeField] public GameObject CoinGoldPrefab { get; private set; }
    [field: SerializeField, Min(1)] public int BaseGoldAmount { get; private set; } = 1;

    [field: Header("Effects")]
    [field: SerializeField] public GameObject PickupYellowPrefab { get; private set; }

    [field: Header("Drop")]
    [field: SerializeField, Min(0f)] public float DropHeight { get; private set; } = 1.2f;
    [field: SerializeField, Min(0f)] public float DropScatterRadius { get; private set; } = 0.8f;
    [field: SerializeField, Min(0f)] public float DropJumpPower { get; private set; } = 1.1f;
    [field: SerializeField, Min(0f)] public float DropDuration { get; private set; } = 0.35f;
    [field: SerializeField, Min(0f)] public float AttractionStartDelay { get; private set; } = 0.2f;
    [field: SerializeField, Min(0f)] public float GroundOffset { get; private set; } = 0.35f;
    [field: SerializeField, Min(0f)] public float GroundSnapRayStartHeight { get; private set; } = 4f;
    [field: SerializeField, Min(0f)] public float GroundSnapRayDistance { get; private set; } = 12f;

    [field: Header("Pickup")]
    [field: SerializeField, Min(0f)] public float AttractionRadius { get; private set; } = 4f;
    [field: SerializeField, Min(0.01f)] public float CollectDistance { get; private set; } = 0.45f;
    [field: SerializeField, Min(0.01f)] public float AttractionSpeed { get; private set; } = 12f;
    [field: SerializeField, Range(0.05f, 1f)] public float AttractionScaleMultiplier { get; private set; } = 0.05f;
    [field: SerializeField, Min(0f)] public float CollectionTargetHeight { get; private set; } = 1f;
}
