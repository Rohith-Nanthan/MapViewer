using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace POI
{
    /// <summary>
    /// Inspector for <see cref="POI_UI"/>: the camera, player and resolution manager, then the Screen and Compass
    /// sections laid out open, with checks for common setup mistakes. In Play Mode it also shows how many markers
    /// each section is showing.
    /// </summary>
    [CustomEditor(typeof(POI_UI))]
    sealed class POI_UIEditor : Editor
    {
        SerializedProperty _camera;
        SerializedProperty _player;
        SerializedProperty _screenResolutionManager;
        SerializedProperty _screen;
        SerializedProperty _compass;

        void OnEnable()
        {
            _camera = serializedObject.FindProperty("m_Camera");
            _player = serializedObject.FindProperty("m_Player");
            _screenResolutionManager = serializedObject.FindProperty("m_ScreenResolutionManager");
            _screen = serializedObject.FindProperty("m_Screen");
            _compass = serializedObject.FindProperty("m_Compass");
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_camera);
            if (_camera.objectReferenceValue == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                    EditorGUILayout.HelpBox($"Uses the main camera, '{mainCamera.name}'.", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("Assign a camera, or tag one MainCamera.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(_player);
            if (_player.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Distances are measured from the camera.", MessageType.Info);

            EditorGUILayout.PropertyField(_screenResolutionManager);
            if (_screenResolutionManager.objectReferenceValue == null)
                EditorGUILayout.HelpBox("In Play Mode, uses the Screen Resolution Manager in the scene, or adds one here.", MessageType.Info);

            DrawSection("Screen", _screen);
            DrawSection("Compass", _compass);

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
                DrawRuntimeInfo((POI_UI)target);
        }

        // The sections are what designers set up, so they are laid out open rather than in foldouts.
        static void DrawSection(string title, SerializedProperty section)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            SerializedProperty field = section.Copy();
            SerializedProperty end = section.GetEndProperty();
            for (bool enterChildren = true; field.NextVisible(enterChildren) && !SerializedProperty.EqualContents(field, end); enterChildren = false)
                EditorGUILayout.PropertyField(field, true);

            DrawSectionChecks(title, section);
        }

        static void DrawSectionChecks(string title, SerializedProperty section)
        {
            var viewport = (RectTransform)section.FindPropertyRelative("m_Viewport").objectReferenceValue;
            var template = (POI_Marker)section.FindPropertyRelative("m_Template").objectReferenceValue;

            if (viewport == null && template == null)
            {
                EditorGUILayout.HelpBox($"Assign a Viewport and a Template to show {title.ToLowerInvariant()} markers, " +
                                        "or leave both empty to skip them.", MessageType.Info);
                return;
            }

            if (viewport == null || template == null)
                EditorGUILayout.HelpBox($"Assign the {(viewport == null ? "Viewport" : "Template")} too.", MessageType.Warning);

            if (viewport != null && viewport.GetComponentInParent<Canvas>(true) == null)
                EditorGUILayout.HelpBox("The Viewport must be inside a Canvas.", MessageType.Error);

            if (viewport != null && viewport.GetComponent<LayoutGroup>() != null)
                EditorGUILayout.HelpBox("Remove the layout group from the Viewport; it would fight the markers for their positions.", MessageType.Warning);

            if (template != null && template.Icon == null)
                EditorGUILayout.HelpBox("The Template's POI Marker has no Icon image assigned.", MessageType.Warning);

            if (!POIUtil.IsValidDistanceFormat(section.FindPropertyRelative("m_DistanceFormat").stringValue))
                EditorGUILayout.HelpBox("Distance Format is not a valid format with {0}; distances will show as plain numbers.", MessageType.Warning);
        }

        static void DrawRuntimeInfo(POI_UI ui)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Active Points", ui.Registry.Count);
                EditorGUILayout.IntField("Screen Markers", ui.ScreenTracker.Count);
                EditorGUILayout.IntField("Compass Markers", ui.CompassTracker.Count);
                EditorGUILayout.ObjectField("Camera In Use", ui.Camera, typeof(Camera), true);
            }
        }
    }
}
