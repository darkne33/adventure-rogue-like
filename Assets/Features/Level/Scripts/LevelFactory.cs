using UnityEngine;
using Zenject;

public class LevelFactory : ILevelFactory
{
    private readonly LevelsConfiguration _levelsConfiguration;
    private readonly DiContainer _container;

    public LevelFactory(LevelsConfiguration levelsConfiguration, DiContainer container)
    {
        _levelsConfiguration = levelsConfiguration;
        _container = container;
    }

    public LevelView CreateLevelView(int levelNumber, Transform parent)
    {
        var level = _container.InstantiatePrefabForComponent<LevelView>(_levelsConfiguration.Levels[levelNumber].LevelView, parent);
        return level;
    }
}