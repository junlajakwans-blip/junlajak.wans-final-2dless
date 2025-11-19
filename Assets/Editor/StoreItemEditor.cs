#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StoreItem))]
public class StoreItemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var item = (StoreItem)target;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("StoreType"));
        EditorGUILayout.Space(6);

        // Display
        EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("DisplayName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Icon"));
        EditorGUILayout.Space(6);

        // Price section
        EditorGUILayout.LabelField("Price", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("SpendCurrency"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Price"));
        EditorGUILayout.Space(6);

        // Section by StoreType
        switch (item.StoreType)
        {
            case StoreType.Exchange:
                EditorGUILayout.LabelField("Reward (Exchange Only)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RewardCurrency"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RewardAmount"));
                break;

            case StoreType.Map:
                EditorGUILayout.LabelField("Unlock (Map Only)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mapType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("UnlockedByDefault"));
                break;

            case StoreType.Upgrade:
                EditorGUILayout.LabelField("Upgrade (Upgrade Only)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("UseLevelScaling"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("PriceMultiplier"));
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
