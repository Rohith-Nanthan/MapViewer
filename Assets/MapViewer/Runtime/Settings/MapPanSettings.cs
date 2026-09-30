using System;
using UnityEngine;

namespace Maps
{
    /// <summary>Tuning for panning the map, set in the Pan section of <see cref="MapViewer"/>.</summary>
    [Serializable]
    public sealed class MapPanSettings
    {
        [Tooltip("Pan speed at full stick or D-pad deflection, in viewport lengths per second " +
                 "(measured along the viewport's shorter side).")]
        [SerializeField, Min(0f)] float m_Speed = 1f;

        [Tooltip("Seconds for stick panning to ease in and out. 0 responds instantly.")]
        [SerializeField, Min(0f)] float m_SmoothTime = 0.08f;

        [Tooltip("Off: the stick moves the view across the map. On: the stick pushes the map itself.")]
        [SerializeField] bool m_Invert;

        [Tooltip("Multiplier for drag panning. 1 keeps the map locked under the cursor.")]
        [SerializeField, Min(0f)] float m_DragSensitivity = 1f;

        [Tooltip("A drag only starts when the drag button is pressed over the map.")]
        [SerializeField] bool m_DragOnlyOverMap = true;

        public float Speed
        {
            get => m_Speed;
            set => m_Speed = Mathf.Max(0f, value);
        }

        public float SmoothTime
        {
            get => m_SmoothTime;
            set => m_SmoothTime = Mathf.Max(0f, value);
        }

        public bool Invert
        {
            get => m_Invert;
            set => m_Invert = value;
        }

        public float DragSensitivity
        {
            get => m_DragSensitivity;
            set => m_DragSensitivity = Mathf.Max(0f, value);
        }

        public bool DragOnlyOverMap
        {
            get => m_DragOnlyOverMap;
            set => m_DragOnlyOverMap = value;
        }
    }
}
