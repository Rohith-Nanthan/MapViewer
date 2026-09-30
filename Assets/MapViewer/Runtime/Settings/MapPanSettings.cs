using System;
using UnityEngine;

namespace Maps
{
    /// <summary>Tuning for panning the map.</summary>
    [Serializable]
    public sealed class MapPanSettings
    {
        [Tooltip("Pan speed at full stick or D-pad deflection, in viewport lengths per second " +
                 "(measured along the viewport's shorter side).")]
        [SerializeField, Min(0f)] float speed = 1f;

        [Tooltip("Seconds for stick panning to ease in and out. 0 responds instantly.")]
        [SerializeField, Min(0f)] float smoothTime = 0.08f;

        [Tooltip("Off: the stick moves the view across the map. On: the stick pushes the map itself.")]
        [SerializeField] bool invert;

        [Tooltip("Multiplier for drag panning. 1 keeps the map locked under the cursor.")]
        [SerializeField, Min(0f)] float dragSensitivity = 1f;

        public float Speed
        {
            get => speed;
            set => speed = Mathf.Max(0f, value);
        }

        public float SmoothTime
        {
            get => smoothTime;
            set => smoothTime = Mathf.Max(0f, value);
        }

        public bool Invert
        {
            get => invert;
            set => invert = value;
        }

        public float DragSensitivity
        {
            get => dragSensitivity;
            set => dragSensitivity = Mathf.Max(0f, value);
        }
    }
}
