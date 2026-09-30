using System;
using UnityEngine;

namespace Maps
{
    /// <summary>Tuning for zooming the map. Zoom values are multiples of the map's size at 1x.</summary>
    [Serializable]
    public sealed class MapZoomSettings
    {
        [Tooltip("How the map is sized in the viewport at 1x zoom. " +
                 "Fit shows the whole map; Fill covers the whole viewport.")]
        [SerializeField] MapFitMode fitMode = MapFitMode.Fit;

        [Tooltip("Furthest the player can zoom out. 1 shows the map at its Fit Mode size.")]
        [SerializeField, Min(MapViewportModel.MinimumZoom)] float minZoom = 1f;

        [Tooltip("Furthest the player can zoom in.")]
        [SerializeField, Min(MapViewportModel.MinimumZoom)] float maxZoom = 4f;

        [Tooltip("Zoom used when the view is reset.")]
        [SerializeField, Min(MapViewportModel.MinimumZoom)] float defaultZoom = 1f;

        [Tooltip("Zoom speed at full trigger, in doublings per second: 1 doubles or halves the zoom every second.")]
        [SerializeField, Min(0f)] float speed = 1.5f;

        [Tooltip("Zoom multiplier per scroll-wheel notch: 1.25 zooms by 25% per notch.")]
        [SerializeField, Min(1f)] float stepMultiplier = 1.25f;

        [Tooltip("Scroll zoom keeps the point under the cursor in place instead of zooming into the viewport center.")]
        [SerializeField] bool zoomTowardsPointer = true;

        [Tooltip("Seconds for zoom changes to ease in. 0 zooms instantly.")]
        [SerializeField, Min(0f)] float smoothTime = 0.1f;

        public MapFitMode FitMode
        {
            get => fitMode;
            set => fitMode = value;
        }

        /// <summary>Lowest zoom. Raising it above <see cref="MaxZoom"/> raises the maximum too.</summary>
        public float MinZoom
        {
            get => minZoom;
            set
            {
                minZoom = value;
                Validate();
            }
        }

        /// <summary>Highest zoom. Lowering it below <see cref="MinZoom"/> lowers the minimum too.</summary>
        public float MaxZoom
        {
            get => maxZoom;
            set
            {
                maxZoom = value;
                minZoom = Mathf.Min(minZoom, maxZoom);
                Validate();
            }
        }

        /// <summary>Zoom used when the view is reset, kept within the zoom limits.</summary>
        public float DefaultZoom
        {
            get => Mathf.Clamp(defaultZoom, minZoom, maxZoom);
            set => defaultZoom = Mathf.Max(MapViewportModel.MinimumZoom, value);
        }

        public float Speed
        {
            get => speed;
            set => speed = Mathf.Max(0f, value);
        }

        public float StepMultiplier
        {
            get => stepMultiplier;
            set => stepMultiplier = Mathf.Max(1f, value);
        }

        public bool ZoomTowardsPointer
        {
            get => zoomTowardsPointer;
            set => zoomTowardsPointer = value;
        }

        public float SmoothTime
        {
            get => smoothTime;
            set => smoothTime = Mathf.Max(0f, value);
        }

        internal void Validate()
        {
            minZoom = Mathf.Max(MapViewportModel.MinimumZoom, minZoom);
            maxZoom = Mathf.Max(minZoom, maxZoom);
            defaultZoom = Mathf.Max(MapViewportModel.MinimumZoom, defaultZoom);
        }
    }
}
