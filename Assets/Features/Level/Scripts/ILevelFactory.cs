using UnityEngine;

public interface ILevelFactory
{
    Room GetNextBossRoom();
    LevelView CreateLevelView(int levelNumber, Transform parent);
}
