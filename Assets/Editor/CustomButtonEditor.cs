using UnityEditor;
using UnityEditor.UI;

namespace Almace.CustomUI
{
    [CustomEditor(typeof(CustomButton))]
    public class CustomButtonEditor : ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            // Draw default Unity Button fields first
            base.OnInspectorGUI();

            // Now draw our extra properties correctly
            SerializedProperty clickInterval = serializedObject.FindProperty("_clickInterval");
            SerializedProperty hasSound = serializedObject.FindProperty("_hasSound");
            SerializedProperty useAnimation = serializedObject.FindProperty("_useAnimation");
            SerializedProperty useHighlightWiggle = serializedObject.FindProperty("_useHighlightWiggle");
            SerializedProperty onButtonClick = serializedObject.FindProperty("onButtonClick");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Custom Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(clickInterval);
            EditorGUILayout.PropertyField(hasSound);
            EditorGUILayout.PropertyField(useAnimation);
            EditorGUILayout.PropertyField(useHighlightWiggle);
            EditorGUILayout.PropertyField(onButtonClick);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
