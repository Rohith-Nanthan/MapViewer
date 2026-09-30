using System;
using UnityEngine;

namespace Maps
{
    /// <summary>
    /// Tuning for zooming the map, set in the Zoom section of <see cref="MapViewer"/>. Zoom values are
    /// multiples of the map's size at 1x.
    /// </summary>
    [Serializable]
    public sealed class MapZoomSettings
    {
        [Tooltip("How the map is sized in the viewport at 1x zoom. " +
                 "Fit shows the whole map; Fill covers the whole viewport.")]
        [SerializeField] MapFitMode m_FitMode = MapFitMode.Fit;

        [Tooltip("Furthest the player can zoom out. 1 shows the map at its Fit Mode size.")]
        [SerializeField, Min(MapViewportModel.MinimumZoom)] float m_MinZoom = 1f;

        [Tooltip("Furthest the player can zoom in.")]
        [SerializeField, Min(MapViewportModel.MinimumZoom)] float m_MaxZoom = 4f;

        [Tooltip("Zoom used when the view is reset.")]
        [SerializeField, Min(MapViewportModel.MinimumZoom)] float m_DefaultZoom = 1f;

        [Tooltip("Zoom speed at full trigger, in doublings per second: 1 doubles or halves the zoom every second.")]
        [SerializeField, Min(0f)] float m_Speed = 1.5f;

        [Tooltip("Zoom multiplier per scroll-wheel notch: 1.25 zooms by 25% per notch.")]
        [SerializeField, Min(1f)] float m_StepMultiplier = 1.25f;

        [Tooltip("Scroll zoom keeps the point under the cursor in place instead of zooming into the viewport center.")]
        [SerializeField] bool m_ZoomTowardsPointer = true;

        [Tooltip("Scroll zoom only applies while the cursor is over the map.")]
        [SerializeField] bool m_ScrollOnlyOverMap = true;

        [Tooltip("Seconds for zoom changes to ease in. 0 zooms instantly.")]
        [SerializeField, Min(0f)] float m_SmoothTime = 0.1f;

        public MapFitMode FitMode
        {
            get => m_FitMode;
            set => m_FitMode = value;
        }

        /// <summary>Lowest zoom. Raising it above <see cref="MaxZoom"/> raises the maximum too.</summary>
        public float MinZoom
        {
            get => m_MinZoom;
            set
            {
                m_MinZoom = value;
                Validate();
            }
        }

        /// <summary>Highest zoom. Lowering it below <see cref="MinZoom"/> lowers the minimum too.</summary>
        public float MaxZoom
        {
            get => m_MaxZoom;
            set
            {
                m_MaxZoom = value;
                m_MinZoom = Mathf.Min(m_MinZoom, m_MaxZoom);
                Validate();
            }
        }

        /// <summary>Zoom used when the view is reset, kept within the zoom limits.</summary>
        public float DefaultZoom
        {
            get => Mathf.Clamp(m_DefaultZoom, m_MinZoom, m_MaxZoom);
            set => m_DefaultZoom = Mathf.Max(MapViewportModel.MinimumZoom, value);
        }

        public float Speed
        {
            get => m_Speed;
            set => m_Speed = Mathf.Max(0f, value);
        }

        public float StepMultiplier
        {
            get => m_StepMultiplier;
            set => m_StepMultiplier = Mathf.Max(1f, value);
        }

        public bool ZoomTowardsPointer
        {
            get => m_ZoomTowardsPointer;
            set => m_ZoomTowardsPointer = value;
        }

        public bool ScrollOnlyOverMap
        {
            get => m_ScrollOnlyOverMap;
            set => m_ScrollOnlyOverMap = value;
        }

        public float SmoothTime
        {
            get => m_SmoothTime;
            set => m_SmoothTime = Mathf.Max(0f, value);
        }

        internal void Validate()
        {
            m_MinZoom = Mathf.Max(MapViewportModel.MinimumZoom, m_MinZoom);
            m_MaxZoom = Mathf.Max(m_MinZoom, m_MaxZoom);
            m_DefaultZoom = Mathf.Max(MapViewportModel.MinimumZoom, m_DefaultZoom);
        }
    }
}
