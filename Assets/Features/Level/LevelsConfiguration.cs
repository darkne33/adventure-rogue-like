using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create LevelsConfiguration", fileName = "LevelsConfiguration", order = 0)]
public class LevelsConfiguration : ScriptableObject
{
    [field: SerializeField] public List<LevelView> Levels { get; private set; }
}