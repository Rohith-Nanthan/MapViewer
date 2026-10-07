using System;
using UnityEngine;

namespace POI
{
    [Serializable]
    public sealed class CompassMarkerSettings
    {
        const float k_FullCircleDegrees = 360f;

        [Tooltip("Empty rectangle the markers slide along, e.g. a lane over your compass strip. Markers are spawned " +
                 "as its children and centered vertically in it.")]
        [SerializeField] RectTransform m_Viewport;

        [Tooltip("Prefab spawned for each point of interest, with the distance label under its icon. Its size, " +
                 "children included, always stays inside the viewport.")]
        [SerializeField] POI_Marker m_MarkerPrefab;

        [Tooltip("Degrees of heading the viewport's width spans, centered on where the camera faces. Match your " +
                 "compass strip, so markers line up with its directions.")]
        [SerializeField, Range(1f, k_FullCircleDegrees)] float m_DegreesAcrossViewport = 180f;

        [Tooltip("Space kept between the markers and the left and right edges of the viewport, in canvas units.")]
        [SerializeField, Min(0f)] float m_Padding = 8f;

        [Tooltip("Text of the distance label. {0} is the distance from the player in whole meters.")]
        [SerializeField] string m_DistanceFormat = POI_Marker.DefaultDistanceFormat;

        public CompassMarkerSettings()
        {
        }

        public CompassMarkerSettings(RectTransform viewport, POI_Marker markerPrefab)
        {
            m_Viewport = viewport;
            m_MarkerPrefab = markerPrefab;
        }

        public RectTransform Viewport
        {
            get => m_Viewport;
            set => m_Viewport = value;
        }

        public POI_Marker MarkerPrefab
        {
            get => m_MarkerPrefab;
            set => m_MarkerPrefab = value;
        }

        public float DegreesAcrossViewport
        {
            get => m_DegreesAcrossViewport;
            set => m_DegreesAcrossViewport = Mathf.Clamp(value, 1f, k_FullCircleDegrees);
        }

        public float Padding
        {
            get => m_Padding;
            set => m_Padding = Mathf.Max(0f, value);
        }

        public string DistanceFormat
        {
            get => m_DistanceFormat;
            set => m_DistanceFormat = value;
        }
    }
}
