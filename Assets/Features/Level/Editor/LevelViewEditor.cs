#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelView))]
[CanEditMultipleObjects]
public sealed class LevelViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Assign the start-room prefab and room prefabs with their Grid Position. " +
            "The runtime creates room instances, disables their authored doors, and enables only " +
            "the directions required by the grid topology. Each room prefab must already contain " +
            "its directional RoomDoor objects; no doors are instantiated or replaced. " +
            "Each RoomDoor selects EnemyDoor or RewardDoor from the destination room type. " +
            "Enemy and Exit rooms require an Enemy Configuration assigned on their room node.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (!GUILayout.Button("Validate Level Setup"))
            return;

        foreach (UnityEngine.Object selectedTarget in targets)
        {
            var level = (LevelView)selectedTarget;
            try
            {
                level.ValidateAuthoring();
                Debug.Log($"{level.name}: level setup is valid.", level);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, level);
            }
        }
    }
}
#endif
