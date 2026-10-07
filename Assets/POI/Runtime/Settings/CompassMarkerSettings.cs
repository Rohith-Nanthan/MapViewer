using System;
using UnityEngine;

namespace POI
{
    /// <summary>
    /// Where compass markers are shown, set in the Compass section of <see cref="POI_UI"/>. Markers slide
    /// horizontally with the camera's heading, centered when the camera faces their point of interest, and
    /// stop at the edges for points outside the field of view.
    /// </summary>
    [Serializable]
    public sealed class CompassMarkerSettings
    {
        /// <summary>Widest field of view: the whole way around.</summary>
        public const float MaxFieldOfView = 360f;

        [Tooltip("Rectangle the markers slide along, e.g. over the compass bar. Markers are created as its children " +
                 "and keep the height of the template.")]
        [SerializeField] RectTransform m_Viewport;

        [Tooltip("Marker copied for each point of interest. Give it a distance label below the icon to show how far " +
                 "away the point is. Its size, children included, always stays inside the viewport.")]
        [SerializeField] POI_Marker m_Template;

        [Tooltip("Degrees of heading the viewport's width spans, centered on where the camera faces. Match your " +
                 "compass graphic, so markers line up with its directions.")]
        [SerializeField, Range(1f, MaxFieldOfView)] float m_FieldOfView = 180f;

        [Tooltip("Space kept between the markers and the left and right edges of the viewport, in canvas units.")]
        [SerializeField, Min(0f)] float m_Padding = 8f;

        [Tooltip("Text for markers that have a distance label. {0} is the distance from the player in whole meters.")]
        [SerializeField] string m_DistanceFormat = POI_Marker.DefaultDistanceFormat;

        public CompassMarkerSettings()
        {
        }

        public CompassMarkerSettings(RectTransform viewport, POI_Marker template)
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

        /// <summary>Degrees of heading the viewport's width spans, between 1 and <see cref="MaxFieldOfView"/>.</summary>
        public float FieldOfView
        {
            get => m_FieldOfView;
            set => m_FieldOfView = Mathf.Clamp(value, 1f, MaxFieldOfView);
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
