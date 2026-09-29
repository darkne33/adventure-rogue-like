using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class LevelFactory : ILevelFactory
{
    private readonly LevelsConfiguration _levelsConfiguration;
    private readonly DiContainer _container;
    private readonly Dictionary<int, int> _openingCombatRoomCounts = new();

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

        if (levelNumber == 0)
            _openingCombatRoomCounts.Clear();

        bool isOpeningLevel = levelNumber < LevelsConfiguration.AuthoredLevelCount;
        LevelRoomNode[] generatedRooms = isOpeningLevel
            ? ProceduralLevelGenerator.GenerateFromExample(_levelsConfiguration, levelNumber)
            : _levelsConfiguration.IsProceduralLevel(levelNumber)
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
                _levelsConfiguration.GetCombatProgressOffset(levelNumber, _openingCombatRoomCounts));
            if (isOpeningLevel)
                _openingCombatRoomCounts[levelNumber] = levelView.GetCombatRoomsToExit();
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
