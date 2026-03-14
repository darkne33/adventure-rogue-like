using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create LevelsConfiguration", fileName = "LevelsConfiguration", order = 0)]
public class LevelsConfiguration : ScriptableObject
{
    [field: SerializeField] public LayerMask GroundLayer { get; private set; }
    [field: SerializeField] public LayerMask ObstacleLayer { get; private set; }
    [field: SerializeField] public List<LevelSettings> Levels { get; private set; }
}

[Serializable]
public class LevelSettings
{
    [field: SerializeField] public EnemyFactoryConfiguration EnemyFactoryConfiguration { get; private set; }
    [field: SerializeField] public LevelView LevelView { get; private set; }
}
