using System;
using UnityEngine;

namespace POI
{
    /// <summary>
    /// Where screen markers are shown, set in the Screen section of <see cref="POI_UI"/>. A marker sits over its
    /// point of interest while it is in view, and hugs the viewport's edge on the point's side while it is not.
    /// </summary>
    [Serializable]
    public sealed class ScreenMarkerSettings
    {
        [Tooltip("Rectangle the markers move within, e.g. the screen's safe area. Markers are created as its children.")]
        [SerializeField] RectTransform m_Viewport;

        [Tooltip("Marker copied for each point of interest. Its size, children included, always stays inside the viewport.")]
        [SerializeField] POI_Marker m_Template;

        [Tooltip("Space kept between the markers and each edge of the viewport, in canvas units.")]
        [SerializeField] EdgePadding m_Padding = new EdgePadding(16f);

        [Tooltip("Text for markers that have a distance label. {0} is the distance from the player in whole meters.")]
        [SerializeField] string m_DistanceFormat = POI_Marker.DefaultDistanceFormat;

        public ScreenMarkerSettings()
        {
        }

        public ScreenMarkerSettings(RectTransform viewport, POI_Marker template)
        {
            m_Viewport = viewport;
            m_Template = template;
        }

        public RectTransform Viewport
        {
            get => m_Viewport;
            set => m_Viewport = value;
        }

        public POI_Marker Template
        {
            get => m_Template;
            set => m_Template = value;
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
