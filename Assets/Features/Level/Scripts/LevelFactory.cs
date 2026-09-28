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

        LevelRoomNode[] generatedRooms = _levelsConfiguration.IsProceduralLevel(levelNumber)
            ? ProceduralLevelGenerator.Generate(_levelsConfiguration, levelNumber)
            : null;
        LevelView levelView = _container.InstantiatePrefabForComponent<LevelView>(levelSettings.LevelView, parent);
        try
        {
            if (generatedRooms != null)
            {
                levelView.name = $"Level_{levelNumber + 1}";
                levelView.Configure(generatedRooms);
            }

            levelView.Initialize(
                _container,
                _levelsConfiguration.HasLevel(levelNumber + 1),
                _levelsConfiguration.EnemyRoomScalingConfiguration,
                _levelsConfiguration.GetCombatProgressOffset(levelNumber));
            return levelView;
        }
        catch
        {
            levelView.gameObject.SetActive(false);
            Object.Destroy(levelView.gameObject);
            throw;
        }
    }
}
