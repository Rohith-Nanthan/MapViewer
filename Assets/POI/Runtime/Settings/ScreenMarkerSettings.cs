using System;
using UnityEngine;

namespace POI
{
    [Serializable]
    public sealed class ScreenMarkerSettings
    {
        [Tooltip("Rectangle the markers move within, e.g. the screen's safe area. Markers are spawned as its children.")]
        [SerializeField] RectTransform m_Viewport;

        [Tooltip("Prefab spawned for each point of interest. Its size, children included, always stays inside the viewport.")]
        [SerializeField] POI_Marker m_MarkerPrefab;

        [Tooltip("Space kept between the markers and each edge of the viewport, in canvas units.")]
        [SerializeField] EdgePadding m_Padding = new EdgePadding(16f);

        [Tooltip("Text for markers that have a distance label. {0} is the distance from the player in whole meters.")]
        [SerializeField] string m_DistanceFormat = POI_Marker.DefaultDistanceFormat;

        public ScreenMarkerSettings()
        {
        }

        public ScreenMarkerSettings(RectTransform viewport, POI_Marker markerPrefab)
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

        public EdgePadding Padding
        {
            get => m_Padding;
            set => m_Padding = value;
        }

        public string DistanceFormat
        {
            get => m_DistanceFormat;
            set => m_DistanceFormat = value;
        }
    }
}
