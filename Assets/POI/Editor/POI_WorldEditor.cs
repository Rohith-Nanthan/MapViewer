using UnityEditor;
using UnityEngine;

namespace POI
{
    [CustomEditor(typeof(POI_World)), CanEditMultipleObjects]
    sealed class POI_WorldEditor : Editor
    {
        const float k_GizmoRadius = 0.5f;
        static readonly Color k_ActiveColor = new Color(1f, 0.8f, 0.2f);
        static readonly Color k_InactiveColor = new Color(0.6f, 0.6f, 0.6f);

        SerializedProperty _icon;
        SerializedProperty _position;

        void OnEnable()
        {
            _icon = serializedObject.FindProperty("m_Icon");
            _position = serializedObject.FindProperty("m_Position");
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_icon);
            if (!_icon.hasMultipleDifferentValues && _icon.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign the icon shown on screen and on the compass.", MessageType.Warning);

            EditorGUILayout.PropertyField(_position);
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button(new GUIContent("Use Transform Position", "Move the point to this GameObject's current position.")))
                UseTransformPositions();

            if (Application.isPlaying)
                DrawRuntimeControls();
        }

        void OnSceneGUI()
        {
            var poi = (POI_World)target;

            EditorGUI.BeginChangeCheck();
            Vector3 position = Handles.PositionHandle(poi.Position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(poi, "Move Point of Interest");
                poi.Position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(poi);
            }

            Handles.Label(poi.Position + Vector3.up * (k_GizmoRadius * 2f), poi.name, EditorStyles.boldLabel);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmo(POI_World poi, GizmoType gizmoType)
        {
            bool isSelected = (gizmoType & GizmoType.Selected) != 0;
            Color color = Application.isPlaying && poi.IsActive ? k_ActiveColor : k_InactiveColor;
            color.a = isSelected ? 1f : 0.5f;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(poi.Position, k_GizmoRadius);

            // The position is independent of the transform, so show how the two relate.
            if (isSelected)
                Gizmos.DrawLine(poi.transform.position, poi.Position);
        }

        void UseTransformPositions()
        {
            foreach (Object poiObject in targets)
            {
                var poi = (POI_World)poiObject;
                var serializedPoi = new SerializedObject(poi);
                serializedPoi.FindProperty("m_Position").vector3Value = poi.transform.position;
                serializedPoi.ApplyModifiedProperties();
            }
        }

        void DrawRuntimeControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            if (targets.Length == 1)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Toggle("Is Active", ((POI_World)target).IsActive);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Activate"))
                {
                    foreach (Object poi in targets)
                        ((POI_World)poi).Activate();
                }

                if (GUILayout.Button("DeActivate"))
                {
                    foreach (Object poi in targets)
                        ((POI_World)poi).DeActivate();
                }
            }
        }
    }
}
