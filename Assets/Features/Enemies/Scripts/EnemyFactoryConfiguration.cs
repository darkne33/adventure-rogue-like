using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

[CreateAssetMenu(menuName = "Create EnemyFactoryConfiguration", fileName = "Configs/Enemies/EnemyFactoryConfiguration", order = 0)]
public class EnemyFactoryConfiguration : ScriptableObject
{
    [field: SerializeField] public List<EnemyPrefabData> EnemyPrefabs { get; set; }

    public GameObject GetEnemyByType(EnemyType enemyType) =>
        EnemyPrefabs.First(x => x.EnemyType == enemyType).WavesConfigurationContainer.Get();
}

[Serializable]
public class EnemyPrefabData
{
    public AddressableLoadContainerGameObject  WavesConfigurationContainer;
    public EnemyType EnemyType;
}