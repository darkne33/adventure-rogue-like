using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Enemies/EnemyFactoryConfiguration", fileName = "EnemyFactoryConfiguration", order = 0)]
public class EnemyFactoryConfiguration : ScriptableObject
{
    [field: SerializeField] public List<EnemyPrefabData> EnemyPrefabs { get; set; }

    public GameObject GetEnemyByType(EnemyType enemyType, int completedEnemyRooms) =>
        EnemyPrefabs.First(x => x.EnemyType == enemyType)
            .GetRandomPrefab(completedEnemyRooms);
}

[Serializable]
public class EnemyPrefabData
{
    public AddressableLoadContainerGameObject NormalPrefabContainer = new();
    public AddressableLoadContainerGameObject ElitePrefabContainer = new();

    [Range(0f, 1f)] public float EliteSpawnChance;
    [Min(0)] public int RequiredCompletedRoomsForElite;
    public EnemyType EnemyType;

    public bool HasElitePrefab =>
        ElitePrefabContainer?.AssetReference != null &&
        ElitePrefabContainer.AssetReference.RuntimeKeyIsValid();

    public GameObject GetRandomPrefab(int completedEnemyRooms)
    {
        if (HasElitePrefab &&
            completedEnemyRooms >= RequiredCompletedRoomsForElite &&
            UnityEngine.Random.value < EliteSpawnChance)
        {
            return ElitePrefabContainer.Get();
        }

        return NormalPrefabContainer.Get();
    }
}
