using UnityEngine;
using UnityEngine.Serialization;

namespace Features.Relics.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Relics/Relic Chest")]
    public sealed class RelicChestConfiguration : ScriptableObject
    {
        [field: SerializeField] public GameObject ChestPrefab { get; private set; }
        [field: SerializeField] public GameObject RelicPickupPrefab { get; private set; }
        [field: SerializeField, Min(0)] public int MinChestsPerLevel { get; private set; } = 1;
        [field: FormerlySerializedAs("<ChestsPerLevel>k__BackingField")]
        [field: SerializeField, Min(0)] public int MaxChestsPerLevel { get; private set; } = 2;
        [field: SerializeField, Min(0f)] public float ChestRoomOffsetRadius { get; private set; } = 4f;
        [field: SerializeField, Min(1)] public int ChestSpawnAttempts { get; private set; } = 24;
        [field: SerializeField, Min(0f)] public float GroundRayStartHeight { get; private set; } = 50f;
        [field: SerializeField, Min(0f)] public float GroundRayDistance { get; private set; } = 100f;
        [field: SerializeField] public float ChestSpawnHeight { get; private set; } = 0.45f;
        [field: SerializeField, Min(0f)] public float ObstacleCheckRadius { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float ObstacleCheckHeight { get; private set; } = 0.6f;
        [field: SerializeField] public float InteractDistance { get; private set; } = 4f;
        [field: SerializeField] public float RelicDropHeight { get; private set; } = 2.2f;
        [field: SerializeField] public float RelicPickupDistance { get; private set; } = 3f;
    }
}
