using UnityEngine;
using Zenject;

public class LevelFactory : ILevelFactory
{
    private readonly LevelsConfiguration _levelsConfiguration;
    private readonly IRogueLikeRuntimeDataService _runtimeData;
    private readonly CharacterWallet _characterWallet;
    private readonly DiContainer _container;

    public LevelFactory(LevelsConfiguration levelsConfiguration, DiContainer container,
        IRogueLikeRuntimeDataService runtimeData, CharacterWallet characterWallet)
    {
        _levelsConfiguration = levelsConfiguration;
        _container = container;
        _runtimeData = runtimeData;
        _characterWallet = characterWallet;
    }

    public LevelView CreateLevelView(int levelNumber, Transform parent)
    {
        LevelSettings levelSettings = _levelsConfiguration.GetLevel(levelNumber);
        LevelRoomNode[] rooms = ProceduralLevelGenerator.Generate(_levelsConfiguration, levelNumber,
            _characterWallet.Gold.Count, _characterWallet.Keys.Count);
        LevelView levelView = _container.InstantiatePrefabForComponent<LevelView>(levelSettings.LevelView, parent);
        try
        {
            levelView.name = $"Level_{levelNumber + 1}";
            levelView.Configure(rooms, _levelsConfiguration.RoomCatalog);
            levelView.Initialize(_container, _levelsConfiguration.HasLevel(levelNumber + 1),
                _levelsConfiguration.EnemyRoomScalingConfiguration, _runtimeData.VisitedCombatRoomsCount);
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
