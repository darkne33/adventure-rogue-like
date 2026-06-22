#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PrebuiltLevelsGenerator
{
    private const int LevelsCount = 3;
    private const int RoomsPerLevel = 4;
    private const float RoomSpacing = 160f;
    private const string LevelsRoot = "Assets/Features/Level";
    private const string BaseRoomPath =
        "Assets/Features/Level/Editor/Templates/DefaultRoomTemplate.prefab";
    private const string StartRoomPath =
        "Assets/Features/Level/Level_1/Rooms/RoomStart.prefab";
    private const string DoorPrefabPath =
        "Assets/Features/RoomGates/Prefabs/DefaultRoom.prefab";
    private const string CoinDoorPrefabPath =
        "Assets/Features/RoomGates/Prefabs/CoinRoomGate.prefab";
    private const string ConfigurationPath =
        "Assets/Features/Level/LevelsConfiguration.asset";

    private static readonly Vector2Int StartRoomGridPosition = new(0, -1);

    private static readonly Vector2Int[][] RoomLayouts =
    {
        new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        },
        new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        },
        new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1)
        }
    };

    private static readonly int[] ExitRoomIndices = { 3, 3, 3 };
    private static readonly int[] RewardRoomIndices = { 1, 1, 1 };
    private static readonly RoomDirection[] ExitDirections =
    {
        RoomDirection.Up,
        RoomDirection.Up,
        RoomDirection.Up
    };

    [MenuItem("Tools/Little Rush/Levels/Generate Prebuilt Grid Levels")]
    public static void Generate()
    {
        LevelsConfiguration configuration =
            AssetDatabase.LoadAssetAtPath<LevelsConfiguration>(ConfigurationPath);
        if (configuration == null)
            throw new InvalidOperationException("Levels configuration asset is missing.");

        ConfigureRoomPrefabs();

        GameObject baseRoom = AssetDatabase.LoadAssetAtPath<GameObject>(BaseRoomPath);
        GameObject startRoom = AssetDatabase.LoadAssetAtPath<GameObject>(StartRoomPath);
        if (baseRoom == null || startRoom == null)
            throw new InvalidOperationException("Base room or start room prefab is missing.");

        try
        {
            var levelViews = new List<LevelView>(LevelsCount);

            for (int levelNumber = 1; levelNumber <= LevelsCount; levelNumber++)
            {
                EnsureLevelFolders(levelNumber);
                GameObject[] roomPrefabs = CreateRoomPrefabs(baseRoom, levelNumber);
                levelViews.Add(CreateLevelPrefab(startRoom, roomPrefabs, levelNumber));
            }

            UpdateConfiguration(configuration, levelViews);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();

            Debug.Log(
                $"Generated {LevelsCount} grid levels with {RoomsPerLevel} rooms each.");
        }
        catch
        {
            AssetDatabase.Refresh();
            throw;
        }
    }

    [MenuItem("Tools/Little Rush/Levels/Validate Prebuilt Grid Levels")]
    public static void Validate()
    {
        LevelsConfiguration configuration =
            AssetDatabase.LoadAssetAtPath<LevelsConfiguration>(ConfigurationPath);
        if (configuration?.Levels == null || configuration.Levels.Count == 0)
            throw new InvalidOperationException(
                "Levels configuration must contain at least one level.");

        for (int index = 0; index < configuration.Levels.Count; index++)
        {
            int levelNumber = index + 1;
            LevelView levelView = configuration.Levels[index]?.LevelView;

            if (levelView == null)
                throw new InvalidOperationException(
                    $"Level {levelNumber} is not assigned in the configuration.");

            ValidateStartRoom(levelView, levelNumber);
            ValidateLevelRooms(levelView, levelNumber,
                hasNextLevel: index < configuration.Levels.Count - 1);
        }

        Debug.Log($"Validated {configuration.Levels.Count} configured grid levels.");
    }

    private static void ConfigureRoomPrefabs()
    {
        ConfigureCombatRoomPrefab();
        ConfigureStartRoomPrefab();
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureCombatRoomPrefab()
    {
        GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
        if (doorPrefab == null)
            throw new InvalidOperationException("Room door prefab is missing.");

        GameObject root = PrefabUtility.LoadPrefabContents(BaseRoomPath);
        try
        {
            DefaultRoom room = root.GetComponent<DefaultRoom>();
            if (room?.RoomData is not DefaultEnemiesRoomData roomData)
                throw new InvalidOperationException(
                    "Base combat room must contain DefaultEnemiesRoomData.");

            var doors = root.GetComponentsInChildren<RoomDoor>(true).ToList();
            if (doors.Count == 3)
                doors.Add(CreateRightDoor(doorPrefab, doors));

            if (doors.Count != 4)
                throw new InvalidOperationException(
                    $"Base combat room must contain four doors, but contains {doors.Count}.");

            AssignDoorDirections(doors);
            roomData.RoomDoors = OrderDoors(doors);
            PrefabUtility.SaveAsPrefabAsset(root, BaseRoomPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureStartRoomPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(StartRoomPath);
        try
        {
            Room room = root.GetComponent<Room>();
            if (room?.RoomData is not StartRoomData roomData)
                throw new InvalidOperationException("Start room must contain StartRoomData.");

            var doors = root.GetComponentsInChildren<RoomDoor>(true).ToList();
            if (doors.Count != 3)
                throw new InvalidOperationException(
                    $"Start room must contain three doors, but contains {doors.Count}.");

            AssignDoorDirections(doors);
            roomData.RoomDoors = OrderDoors(doors);
            PrefabUtility.SaveAsPrefabAsset(root, StartRoomPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static RoomDoor CreateRightDoor(GameObject doorPrefab, IReadOnlyList<RoomDoor> doors)
    {
        RoomDoor downDoor = doors.OrderBy(door => door.transform.localPosition.z).First();
        RoomDoor upDoor = doors.OrderByDescending(door => door.transform.localPosition.z).First();
        RoomDoor leftDoor = doors.OrderBy(door => door.transform.localPosition.x).First();

        float centerX = (downDoor.transform.localPosition.x + upDoor.transform.localPosition.x) * 0.5f;
        float rightX = centerX + (centerX - leftDoor.transform.localPosition.x);

        GameObject rightDoorObject =
            PrefabUtility.InstantiatePrefab(doorPrefab, leftDoor.transform.parent) as GameObject;
        if (rightDoorObject == null)
            throw new InvalidOperationException("Could not instantiate the right room door.");

        rightDoorObject.name = "RightDoor";
        rightDoorObject.transform.localPosition =
            new Vector3(rightX, leftDoor.transform.localPosition.y, leftDoor.transform.localPosition.z);
        rightDoorObject.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);
        rightDoorObject.transform.localScale = Vector3.one;

        return rightDoorObject.GetComponent<RoomDoor>();
    }

    private static void AssignDoorDirections(IReadOnlyCollection<RoomDoor> doors)
    {
        float minX = doors.Min(door => door.transform.localPosition.x);
        float maxX = doors.Max(door => door.transform.localPosition.x);
        float minZ = doors.Min(door => door.transform.localPosition.z);
        float maxZ = doors.Max(door => door.transform.localPosition.z);
        var center = new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);

        foreach (RoomDoor door in doors)
        {
            Vector3 position = door.transform.localPosition;
            float horizontalDistance = Mathf.Abs(position.x - center.x);
            float verticalDistance = Mathf.Abs(position.z - center.y);

            RoomDirection direction = horizontalDistance > verticalDistance
                ? position.x < center.x ? RoomDirection.Left : RoomDirection.Right
                : position.z < center.y ? RoomDirection.Down : RoomDirection.Up;

            door.gameObject.name = $"{direction}Door";
            door.SetDirection(direction);
            EditorUtility.SetDirty(door);
        }

        if (doors.Select(door => door.Direction).Distinct().Count() != doors.Count)
            throw new InvalidOperationException("Room contains duplicate door directions.");
    }

    private static RoomDoor[] OrderDoors(IEnumerable<RoomDoor> doors) =>
        doors.OrderBy(door => door.Direction).ToArray();

    private static GameObject[] CreateRoomPrefabs(GameObject baseRoom, int levelNumber)
    {
        var roomPrefabs = new GameObject[RoomsPerLevel];

        for (int roomIndex = 0; roomIndex < RoomsPerLevel; roomIndex++)
        {
            int roomNumber = roomIndex + 1;
            string roomPath = GetRoomPath(levelNumber, roomNumber);
            GameObject roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(roomPath);

            if (roomPrefab == null)
                roomPrefab = CreateRoomPrefabVariant(baseRoom, roomPath, levelNumber, roomNumber);

            roomPrefabs[roomIndex] = roomPrefab;
        }

        return roomPrefabs;
    }

    private static GameObject CreateRoomPrefabVariant(GameObject baseRoom, string path,
        int levelNumber, int roomNumber)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(baseRoom) as GameObject;
        if (instance == null)
            throw new InvalidOperationException($"Could not instantiate base room {BaseRoomPath}.");

        try
        {
            instance.name = $"DefaultRoom_Level_{levelNumber}_{roomNumber}";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path, out bool success);
            if (!success || prefab == null)
                throw new InvalidOperationException($"Could not create room prefab at {path}.");

            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static LevelView CreateLevelPrefab(GameObject startRoomPrefab,
        IReadOnlyList<GameObject> roomPrefabs, int levelNumber)
    {
        var levelRoot = new GameObject($"Level_{levelNumber}");
        GameObject defaultDoorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
        GameObject coinDoorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinDoorPrefabPath);

        if (defaultDoorPrefab == null || coinDoorPrefab == null)
            throw new InvalidOperationException("Default or coin room door prefab is missing.");

        try
        {
            LevelView levelView = levelRoot.AddComponent<LevelView>();
            Room startRoom = InstantiateChild<Room>(startRoomPrefab, levelRoot.transform,
                $"RoomStart_Level_{levelNumber}");
            startRoom.transform.localPosition = ToWorldPosition(StartRoomGridPosition);

            Vector2Int[] layout = RoomLayouts[levelNumber - 1];
            var roomPositions = new HashSet<Vector2Int>(layout)
            {
                StartRoomGridPosition
            };
            bool hasNextLevel = levelNumber < LevelsCount;
            var rooms = new DefaultRoom[RoomsPerLevel];
            for (int roomIndex = 0; roomIndex < RoomsPerLevel; roomIndex++)
            {
                rooms[roomIndex] = InstantiateChild<DefaultRoom>(roomPrefabs[roomIndex],
                    levelRoot.transform, $"DefaultRoom_Level_{levelNumber}_{roomIndex + 1}");
                rooms[roomIndex].transform.localPosition = ToWorldPosition(layout[roomIndex]);
            }

            var roomNodes = new LevelRoomNode[rooms.Length];
            for (int roomIndex = 0; roomIndex < rooms.Length; roomIndex++)
            {
                bool isLevelExit =
                    roomIndex == ExitRoomIndices[levelNumber - 1];
                bool isRewardRoom =
                    roomIndex == RewardRoomIndices[levelNumber - 1];
                roomNodes[roomIndex] = new LevelRoomNode(
                    rooms[roomIndex],
                    layout[roomIndex],
                    isLevelExit,
                    ExitDirections[levelNumber - 1]);

                RemoveUnusedDoors(rooms[roomIndex], layout[roomIndex], roomPositions,
                    hasNextLevel && isLevelExit, ExitDirections[levelNumber - 1]);
                ConfigureRoomKind(rooms[roomIndex], isRewardRoom);
            }

            RemoveUnusedDoors(startRoom, StartRoomGridPosition, roomPositions,
                isLevelExit: false, default);
            ApplyDestinationDoorPrefabs(startRoom, StartRoomGridPosition, layout,
                defaultDoorPrefab, coinDoorPrefab, levelNumber);
            for (int roomIndex = 0; roomIndex < rooms.Length; roomIndex++)
            {
                ApplyDestinationDoorPrefabs(rooms[roomIndex], layout[roomIndex], layout,
                    defaultDoorPrefab, coinDoorPrefab, levelNumber);
            }

            levelView.Configure(startRoom, StartRoomGridPosition, roomNodes);

            string levelPath = GetLevelPath(levelNumber);
            GameObject savedPrefab =
                PrefabUtility.SaveAsPrefabAsset(levelRoot, levelPath, out bool success);
            if (!success || savedPrefab == null)
                throw new InvalidOperationException($"Could not create level prefab at {levelPath}.");

            return savedPrefab.GetComponent<LevelView>();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(levelRoot);
        }
    }

    private static T InstantiateChild<T>(GameObject prefab, Transform parent, string objectName)
        where T : Component
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
            throw new InvalidOperationException($"Could not instantiate prefab {prefab.name}.");

        instance.name = objectName;
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        T component = instance.GetComponent<T>();
        if (component == null)
            throw new InvalidOperationException(
                $"Prefab {prefab.name} does not contain {typeof(T).Name}.");

        return component;
    }

    private static void RemoveUnusedDoors(Room room, Vector2Int gridPosition,
        ISet<Vector2Int> roomPositions, bool isLevelExit,
        RoomDirection levelExitDirection)
    {
        RoomDoor[] doors = room.RoomData.RoomDoors;
        if (doors == null)
            throw new InvalidOperationException($"{room.name} does not contain configured doors.");

        var retainedDoors = new List<RoomDoor>(doors.Length);
        foreach (RoomDoor door in doors)
        {
            if (door == null)
                continue;

            bool hasNeighbour =
                roomPositions.Contains(gridPosition + door.Direction.ToGridOffset());
            bool isExitDoor = isLevelExit && door.Direction == levelExitDirection;
            if (hasNeighbour || isExitDoor)
            {
                retainedDoors.Add(door);
                continue;
            }

            UnityEngine.Object.DestroyImmediate(door.gameObject);
        }

        room.RoomData.RoomDoors = retainedDoors.ToArray();
        PrefabUtility.RecordPrefabInstancePropertyModifications(room);
    }

    private static void ConfigureRoomKind(DefaultRoom room, bool isRewardRoom)
    {
        if (room.RoomData?.RoomDoors == null)
            throw new InvalidOperationException($"{room.name} does not contain configured doors.");

        if (isRewardRoom)
        {
            room.SetEditorRoomData(new RewardRoomData
            {
                RoomDoors = room.RoomData.RoomDoors
            });
            PrefabUtility.RecordPrefabInstancePropertyModifications(room);
            return;
        }

        if (room.RoomData is not DefaultEnemiesRoomData)
            throw new InvalidOperationException($"{room.name} must contain DefaultEnemiesRoomData.");
    }

    private static void ApplyDestinationDoorPrefabs(Room room, Vector2Int gridPosition,
        IReadOnlyList<Vector2Int> layout, GameObject defaultDoorPrefab,
        GameObject coinDoorPrefab, int levelNumber)
    {
        RoomDoor[] doors = room.RoomData.RoomDoors;
        if (doors == null)
            throw new InvalidOperationException($"{room.name} does not contain configured doors.");

        var updatedDoors = new List<RoomDoor>(doors.Length);
        foreach (RoomDoor door in doors)
        {
            if (door == null)
                continue;

            Vector2Int destination = gridPosition + door.Direction.ToGridOffset();
            bool destinationIsReward = TryGetRoomIndex(layout, destination,
                                           out int destinationRoomIndex) &&
                                       destinationRoomIndex == RewardRoomIndices[levelNumber - 1];
            GameObject desiredPrefab = destinationIsReward ? coinDoorPrefab : defaultDoorPrefab;
            updatedDoors.Add(ReplaceDoorPrefabIfNeeded(door, desiredPrefab));
        }

        room.RoomData.RoomDoors = OrderDoors(updatedDoors);
        PrefabUtility.RecordPrefabInstancePropertyModifications(room);
    }

    private static RoomDoor ReplaceDoorPrefabIfNeeded(RoomDoor door, GameObject desiredPrefab)
    {
        string desiredPath = AssetDatabase.GetAssetPath(desiredPrefab);
        UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(door.gameObject);
        if (source != null && AssetDatabase.GetAssetPath(source) == desiredPath)
            return door;

        Transform doorTransform = door.transform;
        Transform parent = doorTransform.parent;
        Vector3 localPosition = doorTransform.localPosition;
        Quaternion localRotation = doorTransform.localRotation;
        Vector3 localScale = doorTransform.localScale;
        string doorName = door.gameObject.name;
        RoomDirection direction = door.Direction;

        GameObject replacement =
            PrefabUtility.InstantiatePrefab(desiredPrefab, parent) as GameObject;
        if (replacement == null)
            throw new InvalidOperationException($"Could not instantiate door prefab {desiredPrefab.name}.");

        replacement.name = doorName;
        replacement.transform.SetLocalPositionAndRotation(localPosition, localRotation);
        replacement.transform.localScale = localScale;

        RoomDoor replacementDoor = replacement.GetComponent<RoomDoor>();
        if (replacementDoor == null)
            throw new InvalidOperationException($"{desiredPrefab.name} does not contain RoomDoor.");

        replacementDoor.SetDirection(direction);
        UnityEngine.Object.DestroyImmediate(door.gameObject);
        return replacementDoor;
    }

    private static bool TryGetRoomIndex(IReadOnlyList<Vector2Int> layout, Vector2Int position,
        out int roomIndex)
    {
        for (int index = 0; index < layout.Count; index++)
        {
            if (layout[index] != position)
                continue;

            roomIndex = index;
            return true;
        }

        roomIndex = -1;
        return false;
    }

    private static void ValidateStartRoom(LevelView levelView, int levelNumber)
    {
        levelView.ResolveRoomReferences();

        if (levelView.StartRoom?.RoomData is not StartRoomData startRoomData ||
            startRoomData.StartPoint == null ||
            startRoomData.RoomDoors == null ||
            startRoomData.RoomDoors.Length == 0)
            throw new InvalidOperationException(
                $"Level {levelNumber} start room is not configured correctly.");

        ValidateUniqueDirections(startRoomData.RoomDoors,
            $"Level {levelNumber} start room");
    }

    private static void ValidateLevelRooms(LevelView levelView, int levelNumber,
        bool hasNextLevel)
    {
        if (levelView.Rooms == null || levelView.Rooms.Count == 0)
            throw new InvalidOperationException(
                $"Level {levelNumber} must contain at least one enemy room.");

        var positions = new HashSet<Vector2Int> { levelView.StartRoomGridPosition };
        var roomsByPosition = new Dictionary<Vector2Int, Room>
        {
            { levelView.StartRoomGridPosition, levelView.StartRoom }
        };
        int exits = 0;

        foreach (LevelRoomNode node in levelView.Rooms)
        {
            if (node?.Room?.RoomData is not DefaultEnemiesRoomData &&
                node?.Room?.RoomData is not RewardRoomData)
            {
                throw new InvalidOperationException(
                    $"Level {levelNumber} contains an invalid room.");
            }

            RoomData roomData = node.Room.RoomData;
            if (roomData.RoomDoors == null || roomData.RoomDoors.Length == 0)
                throw new InvalidOperationException(
                    $"Level {levelNumber} contains a room without doors.");

            if (roomData is DefaultEnemiesRoomData enemiesRoomData &&
                (enemiesRoomData.EnemyWavesConfiguration == null ||
                 enemiesRoomData.EnemyWavesConfiguration.Length == 0))
            {
                throw new InvalidOperationException(
                    $"Level {levelNumber} contains an invalid enemy room.");
            }

            if (!positions.Add(node.GridPosition))
                throw new InvalidOperationException(
                    $"Level {levelNumber} contains duplicate grid positions.");

            roomsByPosition.Add(node.GridPosition, node.Room);
            ValidateUniqueDirections(roomData.RoomDoors, node.Room.name);

            if ((node.Room.transform.localPosition - ToWorldPosition(node.GridPosition)).sqrMagnitude >
                0.001f)
                throw new InvalidOperationException(
                    $"{node.Room.name} transform does not match its grid position.");

            if (node.IsLevelExit)
            {
                exits++;

                Vector2Int exitPosition =
                    node.GridPosition + node.LevelExitDirection.ToGridOffset();
                if (positions.Contains(exitPosition) ||
                    levelView.Rooms.Any(other => other.GridPosition == exitPosition))
                    throw new InvalidOperationException(
                        $"{node.Room.name} level exit points into another room.");
            }
        }

        if (exits != 1)
            throw new InvalidOperationException(
                $"Level {levelNumber} must contain exactly one exit room.");

        ValidateDoorConnections(levelView, roomsByPosition, levelNumber);
        ValidateConnectivity(roomsByPosition, levelView.StartRoomGridPosition, levelNumber);
    }

    private static void ValidateUniqueDirections(IEnumerable<RoomDoor> doors, string ownerName)
    {
        RoomDirection[] directions = doors.Select(door => door.Direction).ToArray();
        if (directions.Distinct().Count() != directions.Length)
            throw new InvalidOperationException($"{ownerName} contains duplicate door directions.");
    }

    private static void ValidateDoorConnections(LevelView levelView,
        IReadOnlyDictionary<Vector2Int, Room> roomsByPosition, int levelNumber)
    {
        var levelExits = levelView.Rooms
            .Where(node => node.IsLevelExit)
            .ToDictionary(node => node.GridPosition, node => node.LevelExitDirection);

        foreach (KeyValuePair<Vector2Int, Room> roomEntry in roomsByPosition)
        {
            foreach (RoomDoor roomDoor in roomEntry.Value.RoomData.RoomDoors)
            {
                Vector2Int neighbourPosition =
                    roomEntry.Key + roomDoor.Direction.ToGridOffset();
                if (!roomsByPosition.TryGetValue(neighbourPosition, out Room neighbourRoom))
                {
                    bool isLevelExit = levelExits.TryGetValue(roomEntry.Key,
                        out RoomDirection exitDirection) &&
                                       exitDirection == roomDoor.Direction;
                    if (!isLevelExit)
                        throw new InvalidOperationException(
                            $"Level {levelNumber}: {roomEntry.Value.name} contains an unused " +
                            $"{roomDoor.Direction} door.");

                    continue;
                }

                bool hasOppositeDoor = neighbourRoom.RoomData.RoomDoors.Any(
                    door => door.Direction == roomDoor.Direction.Opposite());
                if (!hasOppositeDoor)
                    throw new InvalidOperationException(
                        $"Level {levelNumber}: {roomEntry.Value.name} {roomDoor.Direction} door " +
                        $"does not have an opposite door in {neighbourRoom.name}.");
            }
        }
    }

    private static void ValidateConnectivity(
        IReadOnlyDictionary<Vector2Int, Room> roomsByPosition, Vector2Int startPosition,
        int levelNumber)
    {
        var visited = new HashSet<Vector2Int>();
        var pending = new Queue<Vector2Int>();
        pending.Enqueue(startPosition);

        while (pending.Count > 0)
        {
            Vector2Int position = pending.Dequeue();
            if (!visited.Add(position))
                continue;

            Room room = roomsByPosition[position];
            foreach (RoomDoor roomDoor in room.RoomData.RoomDoors)
            {
                Vector2Int neighbour = position + roomDoor.Direction.ToGridOffset();
                if (roomsByPosition.ContainsKey(neighbour) && !visited.Contains(neighbour))
                    pending.Enqueue(neighbour);
            }
        }

        if (visited.Count != roomsByPosition.Count)
            throw new InvalidOperationException(
                $"Level {levelNumber} contains rooms unreachable from the start.");
    }

    private static void UpdateConfiguration(LevelsConfiguration configuration,
        IReadOnlyList<LevelView> levelViews)
    {
        var serializedConfiguration = new SerializedObject(configuration);
        SerializedProperty levelsProperty =
            serializedConfiguration.FindProperty("<Levels>k__BackingField");

        UnityEngine.Object enemyFactoryConfiguration = levelsProperty.arraySize > 0
            ? levelsProperty.GetArrayElementAtIndex(0)
                .FindPropertyRelative("<EnemyFactoryConfiguration>k__BackingField").objectReferenceValue
            : null;

        levelsProperty.arraySize = Math.Max(levelsProperty.arraySize, LevelsCount);
        for (int index = 0; index < LevelsCount; index++)
        {
            SerializedProperty levelProperty = levelsProperty.GetArrayElementAtIndex(index);
            SerializedProperty enemyFactoryProperty =
                levelProperty.FindPropertyRelative("<EnemyFactoryConfiguration>k__BackingField");

            if (enemyFactoryProperty.objectReferenceValue == null)
                enemyFactoryProperty.objectReferenceValue = enemyFactoryConfiguration;

            levelProperty.FindPropertyRelative("<LevelView>k__BackingField").objectReferenceValue =
                levelViews[index];
        }

        serializedConfiguration.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(configuration);
        AssetDatabase.ForceReserializeAssets(new[] { ConfigurationPath });
    }

    private static void EnsureLevelFolders(int levelNumber)
    {
        string levelFolder = $"{LevelsRoot}/Level_{levelNumber}";
        if (!AssetDatabase.IsValidFolder(levelFolder))
            AssetDatabase.CreateFolder(LevelsRoot, $"Level_{levelNumber}");

        string roomsFolder = $"{levelFolder}/Rooms";
        if (!AssetDatabase.IsValidFolder(roomsFolder))
            AssetDatabase.CreateFolder(levelFolder, "Rooms");
    }

    private static Vector3 ToWorldPosition(Vector2Int gridPosition) =>
        new(gridPosition.x * RoomSpacing, 0f, gridPosition.y * RoomSpacing);

    private static string GetLevelPath(int levelNumber) =>
        $"{LevelsRoot}/Level_{levelNumber}/Level_{levelNumber}.prefab";

    private static string GetRoomPath(int levelNumber, int roomNumber) =>
        $"{LevelsRoot}/Level_{levelNumber}/Rooms/DefaultRoom_Level_{levelNumber}_{roomNumber}.prefab";
}
#endif
