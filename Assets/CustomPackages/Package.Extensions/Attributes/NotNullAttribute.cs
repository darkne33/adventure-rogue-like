using UnityEditor;
using UnityEngine;

namespace CustomPackages.Package.Extensions.Attributes
{
    public class NotNullAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(NotNullAttribute))]
    public class NotNullDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Check if the property is null
            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                property.objectReferenceValue == null)
            {
                // Change the GUI color to red to indicate error
                GUI.color = Color.red;
            }

            EditorGUI.PropertyField(position, property, label);
            GUI.color = Color.white; // Reset color

            EditorGUI.EndProperty();
        }
    }
#endif
}