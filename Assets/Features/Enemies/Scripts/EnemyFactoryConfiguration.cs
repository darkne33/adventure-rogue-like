using Core;
using UnityEngine;

[CreateAssetMenu(menuName = "Create EnemyFactoryConfiguration", fileName = "Configs/Enemies/EnemyFactoryConfiguration", order = 0)]
public class EnemyFactoryConfiguration : ScriptableObject
{
    [field: SerializeField] public AddressableLoadContainerGameObject EnemyContainer { get; private set; }
    [field: SerializeField] public EnemyConfiguration EnemyConfiguration { get; private set; }
}