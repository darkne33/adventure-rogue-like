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
        LevelSettings levelSettings = _levelsConfiguration.GetLevel(levelNumber);
        if (levelSettings.LevelView == null)
            throw new MissingReferenceException($"Level view is not configured for level index {levelNumber}.");

        return _container.InstantiatePrefabForComponent<LevelView>(levelSettings.LevelView, parent);
    }
}
