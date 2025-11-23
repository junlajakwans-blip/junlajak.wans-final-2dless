using UnityEngine;
using UnityEditor;

namespace DUFFDUCK.EditorTools
{
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool previousGUIState = GUI.enabled;
        GUI.enabled = false; // Lock field
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = previousGUIState;
    }
}
}