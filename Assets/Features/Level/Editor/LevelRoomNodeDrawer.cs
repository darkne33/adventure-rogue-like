using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LevelRoomNode))]
public sealed class LevelRoomNodeDrawer : PropertyDrawer
{
    private const string RoomPrefabPropertyName = "_roomPrefab";
    private const string GridPositionPropertyName = "<GridPosition>k__BackingField";
    private const string TypePropertyName = "<Type>k__BackingField";
    private const string ExitDirectionPropertyName = "<LevelExitDirection>k__BackingField";

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight;
        height += GetFieldHeight(property, RoomPrefabPropertyName);
        height += GetFieldHeight(property, GridPositionPropertyName);
        height += GetFieldHeight(property, TypePropertyName);

        SerializedProperty typeProperty = property.FindPropertyRelative(TypePropertyName);
        if ((RoomType)typeProperty.intValue == RoomType.Exit)
            height += GetFieldHeight(property, ExitDirectionPropertyName);

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect line = new(position.x, position.y, position.width,
            EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            line.y += EditorGUIUtility.singleLineHeight +
                      EditorGUIUtility.standardVerticalSpacing;

            DrawField(ref line, property.FindPropertyRelative(RoomPrefabPropertyName));
            DrawField(ref line, property.FindPropertyRelative(GridPositionPropertyName));

            SerializedProperty typeProperty = property.FindPropertyRelative(TypePropertyName);
            DrawField(ref line, typeProperty);

            if ((RoomType)typeProperty.intValue == RoomType.Exit)
            {
                DrawField(ref line,
                    property.FindPropertyRelative(ExitDirectionPropertyName));
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private static float GetFieldHeight(SerializedProperty owner, string propertyName)
    {
        SerializedProperty property = owner.FindPropertyRelative(propertyName);
        return EditorGUI.GetPropertyHeight(property) +
               EditorGUIUtility.standardVerticalSpacing;
    }

    private static void DrawField(ref Rect line, SerializedProperty property)
    {
        line.height = EditorGUI.GetPropertyHeight(property);
        EditorGUI.PropertyField(line, property, true);
        line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
    }
}
