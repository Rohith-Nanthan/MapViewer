using UnityEditor;
using UnityEngine;

namespace Maps
{
    /// <summary>
    /// Inspector for <see cref="MapViewer"/>: flags a missing sprite, edits the settings asset in place and,
    /// in Play Mode, shows the live view with buttons to try the public API.
    /// </summary>
    [CustomEditor(typeof(MapViewer))]
    sealed class MapViewerEditor : Editor
    {
        static bool s_ShowSettings = true;

        SerializedProperty _mapSprite;
        SerializedProperty _settings;
        SerializedProperty _resetViewOnEnable;
        SerializedProperty _view;
        SerializedProperty _input;
        SerializedProperty _events;
        Editor _settingsEditor;

        void OnEnable()
        {
            _mapSprite = serializedObject.FindProperty("m_MapSprite");
            _settings = serializedObject.FindProperty("m_Settings");
            _resetViewOnEnable = serializedObject.FindProperty("m_ResetViewOnEnable");
            _view = serializedObject.FindProperty("m_View");
            _input = serializedObject.FindProperty("m_Input");
            _events = serializedObject.FindProperty("m_Events");
        }

        void OnDisable() => DestroyImmediate(_settingsEditor);

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_mapSprite);
            if (_mapSprite.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign the sprite to show as the map.", MessageType.Warning);

            EditorGUILayout.PropertyField(_resetViewOnEnable);
            DrawSettings();
            EditorGUILayout.PropertyField(_input, true);
            DrawView();
            EditorGUILayout.PropertyField(_events, true);

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
                DrawRuntimeControls((MapViewer)target);
        }

        void DrawView()
        {
            EditorGUILayout.PropertyField(_view, true);
            foreach (string reference in new[] { "m_Viewport", "m_Content", "m_MapImage", "m_MapFitter" })
            {
                if (_view.FindPropertyRelative(reference).objectReferenceValue != null)
                    continue;

                EditorGUILayout.HelpBox("View is missing UI references. Use the MapViewer prefab, which wires them.", MessageType.Error);
                break;
            }
        }

        void DrawSettings()
        {
            EditorGUILayout.PropertyField(_settings);
            var settings = _settings.objectReferenceValue as MapViewerSettings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("No settings asset assigned, so the built-in defaults are used.", MessageType.Info);
                if (GUILayout.Button("Create Settings Asset"))
                    CreateSettingsAsset();
                return;
            }

            s_ShowSettings = EditorGUILayout.Foldout(s_ShowSettings, $"Pan & Zoom ({settings.name}, shared asset)", true);
            if (!s_ShowSettings)
                return;

            CreateCachedEditor(settings, null, ref _settingsEditor);
            using (new EditorGUI.IndentLevelScope())
                _settingsEditor.OnInspectorGUI();
        }

        void CreateSettingsAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Map Viewer Settings", "MapViewerSettings", "asset", "Choose where to save the settings.");
            if (string.IsNullOrEmpty(path))
                return;

            var settings = CreateInstance<MapViewerSettings>();
            AssetDatabase.CreateAsset(settings, path);
            _settings.objectReferenceValue = settings;

            // The modal dialog broke this GUI pass's layout, so save now and start a fresh pass.
            serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
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
