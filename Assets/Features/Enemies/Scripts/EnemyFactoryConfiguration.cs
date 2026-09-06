using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/EnemyFactoryConfiguration", fileName = "EnemyFactoryConfiguration", order = 0)]
public class EnemyFactoryConfiguration : ScriptableObject
{
    [field: Header("Prefab Settings")]
    [field: SerializeField] public List<EnemyPrefabData> EnemyPrefabs { get; set; }

    public GameObject GetEnemyByType(EnemyType enemyType, int roomProgressIndex,
        bool allowElite = true, bool forceElite = false) =>
        EnemyPrefabs.First(x => x.EnemyType == enemyType)
            .GetRandomPrefab(roomProgressIndex, allowElite, forceElite);
}

[Serializable]
public class EnemyPrefabData
{
    public AddressableLoadContainerGameObject NormalPrefabContainer = new();
    public AddressableLoadContainerGameObject ElitePrefabContainer = new();

    [Range(0f, 1f)] public float EliteSpawnChance;
    [Tooltip("Minimum zero-based combat depth for this elite variant.")]
    [Min(0)] public int RequiredCompletedRoomsForElite;
    public EnemyType EnemyType;

    public bool HasElitePrefab =>
        ElitePrefabContainer?.AssetReference != null &&
        ElitePrefabContainer.AssetReference.RuntimeKeyIsValid();

    public GameObject GetRandomPrefab(int roomProgressIndex, bool allowElite = true, bool forceElite = false)
    {
        if (allowElite && HasElitePrefab &&
            roomProgressIndex >= RequiredCompletedRoomsForElite &&
            (forceElite || UnityEngine.Random.value < EliteSpawnChance))
        {
            return ElitePrefabContainer.Get();
        }

        return NormalPrefabContainer.Get();
    }
}
