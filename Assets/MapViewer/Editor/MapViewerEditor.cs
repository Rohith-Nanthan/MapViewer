using UnityEditor;
using UnityEngine;

namespace Maps
{
    /// <summary>
    /// Inspector for <see cref="MapViewer"/>, the one component designers configure: map sprite, pan and zoom
    /// tuning, input actions, UI references and events. In Play Mode it also shows the live view with
    /// buttons to try the public API.
    /// </summary>
    [CustomEditor(typeof(MapViewer))]
    sealed class MapViewerEditor : Editor
    {
        static readonly string[] k_ViewReferences = { "m_Viewport", "m_Content", "m_MapImage", "m_MapFitter" };

        SerializedProperty _mapSprite;
        SerializedProperty _resetViewOnEnable;
        SerializedProperty _pan;
        SerializedProperty _zoom;
        SerializedProperty _input;
        SerializedProperty _view;
        SerializedProperty _events;

        void OnEnable()
        {
            _mapSprite = serializedObject.FindProperty("m_MapSprite");
            _resetViewOnEnable = serializedObject.FindProperty("m_ResetViewOnEnable");
            _pan = serializedObject.FindProperty("m_Pan");
            _zoom = serializedObject.FindProperty("m_Zoom");
            _input = serializedObject.FindProperty("m_Input");
            _view = serializedObject.FindProperty("m_View");
            _events = serializedObject.FindProperty("m_Events");
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_mapSprite);
            if (_mapSprite.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign the sprite to show as the map.", MessageType.Warning);
            EditorGUILayout.PropertyField(_resetViewOnEnable);

            DrawSection("Pan", _pan);
            DrawSection("Zoom", _zoom);

            EditorGUILayout.PropertyField(_input, true);
            EditorGUILayout.PropertyField(_view, true);
            if (HasMissingViewReference())
                EditorGUILayout.HelpBox("View is missing UI references. Use the MapViewer prefab, which wires them.", MessageType.Error);
            EditorGUILayout.PropertyField(_events, true);

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
                DrawRuntimeControls((MapViewer)target);
        }

        // Pan and zoom are what designers tune most, so they are laid out open rather than in foldouts.
        static void DrawSection(string title, SerializedProperty section)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            SerializedProperty field = section.Copy();
            SerializedProperty end = section.GetEndProperty();
            for (bool enterChildren = true; field.NextVisible(enterChildren) && !SerializedProperty.EqualContents(field, end); enterChildren = false)
                EditorGUILayout.PropertyField(field, true);

            EditorGUILayout.Space();
        }

        bool HasMissingViewReference()
        {
            foreach (string reference in k_ViewReferences)
            {
                if (_view.FindPropertyRelative(reference).objectReferenceValue == null)
                    return true;
            }

            return false;
        }

        static void DrawRuntimeControls(MapViewer viewer)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Is Enabled", viewer.IsEnabled);
                EditorGUILayout.FloatField("Zoom", viewer.Model.Zoom);
                EditorGUILayout.Vector2Field("Pan", viewer.Model.Pan);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enable"))
                    viewer.Enable();
                if (GUILayout.Button("Disable"))
                    viewer.Disable();
                if (GUILayout.Button("Reset View"))
                    viewer.ResetView();
            }
        }
    }
}
