using UnityEditor;

namespace Maps
{
    /// <summary>Lays out <see cref="MapViewerSettings"/> as flat Pan, Zoom and Pointer sections.</summary>
    [CustomEditor(typeof(MapViewerSettings))]
    sealed class MapViewerSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSection("Pan", serializedObject.FindProperty("pan"));
            DrawSection("Zoom", serializedObject.FindProperty("zoom"));
            EditorGUILayout.LabelField("Pointer", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requirePointerOverMap"));

            serializedObject.ApplyModifiedProperties();
        }

        static void DrawSection(string title, SerializedProperty section)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            SerializedProperty field = section.Copy();
            SerializedProperty end = section.GetEndProperty();
            for (bool enterChildren = true; field.NextVisible(enterChildren) && !SerializedProperty.EqualContents(field, end); enterChildren = false)
                EditorGUILayout.PropertyField(field, true);

            EditorGUILayout.Space();
        }
    }
}
