using UnityEngine;

public interface ILevelFactory
{
    LevelView CreateLevelView(int levelNumber, Transform parent);
}