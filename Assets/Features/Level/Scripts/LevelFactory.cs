using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class LevelFactory : ILevelFactory
{
    private readonly LevelsConfiguration _levelsConfiguration;
    private readonly IRogueLikeRuntimeDataService _runtimeData;
    private readonly CharacterWallet _characterWallet;
    private readonly DiContainer _container;
    private readonly List<Room> _remainingBossRooms = new();

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
            GetNextBossRoom(), _characterWallet.Gold.Count, _characterWallet.Keys.Count);
        LevelView levelView = _container.InstantiatePrefabForComponent<LevelView>(levelSettings.LevelView, parent);
        try
        {
            levelView.name = $"Level_{levelNumber + 1}";
            levelView.Configure(rooms, _levelsConfiguration.RoomCatalog);
            levelView.Initialize(_container,
                _levelsConfiguration.EnemyRoomScalingConfiguration, _runtimeData.VisitedCombatRoomsCount);
            _remainingBossRooms.RemoveAt(_remainingBossRooms.Count - 1);
            return levelView;
        }
        catch
        {
            levelView.gameObject.SetActive(false);
            Object.Destroy(levelView.gameObject);
            throw;
        }
    }

    // Previewing the next boss reserves the same room until a level initializes successfully.
    public Room GetNextBossRoom()
    {
        if (_remainingBossRooms.Count == 0)
        {
            IReadOnlyList<Room> bossRooms = _levelsConfiguration.RoomCatalog.BossCycle;
            if (bossRooms == null || bossRooms.Count == 0)
                throw new System.InvalidOperationException("The room catalog must contain a boss cycle.");

            var cycle = new List<Room>(bossRooms.Count);
            foreach (Room room in bossRooms)
            {
                if (room == null || room.RoomData is not BossRoomData)
                    throw new System.InvalidOperationException(
                        "The room catalog contains an invalid boss room in its cycle.");
                if (!cycle.Contains(room))
                    cycle.Add(room);
            }

            for (int index = cycle.Count - 1; index > 0; index--)
            {
                int other = Random.Range(0, index + 1);
                (cycle[index], cycle[other]) = (cycle[other], cycle[index]);
            }
            _remainingBossRooms.AddRange(cycle);
        }

        return _remainingBossRooms[_remainingBossRooms.Count - 1];
    }
}
