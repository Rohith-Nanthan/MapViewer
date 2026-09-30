using System;
using UnityEngine;
using UnityEngine.UI;

namespace Maps
{
    /// <summary>
    /// uGUI presentation of the map: shows the sprite at its aspect ratio inside a masked viewport and moves
    /// and scales the content to render pan and zoom. A plain object configured on <see cref="MapViewer"/>.
    /// </summary>
    [Serializable]
    public sealed class MapView : IMapViewportSpace
    {
        [Tooltip("Masked rectangle the map is seen through.")]
        [SerializeField] RectTransform m_Viewport;

        [Tooltip("Child stretched over the viewport. It is moved to pan and scaled to zoom.")]
        [SerializeField] RectTransform m_Content;

        [Tooltip("Image inside the content that displays the map sprite.")]
        [SerializeField] Image m_MapImage;

        [Tooltip("Keeps the map image at the sprite's aspect ratio inside the content.")]
        [SerializeField] AspectRatioFitter m_MapFitter;

        public MapView()
        {
        }

        public MapView(RectTransform viewport, RectTransform content, Image mapImage, AspectRatioFitter mapFitter)
        {
            m_Viewport = viewport;
            m_Content = content;
            m_MapImage = mapImage;
            m_MapFitter = mapFitter;
        }

        /// <summary>Masked rectangle the map is seen through. Overlays such as markers can be parented to it.</summary>
        public RectTransform Viewport => m_Viewport;

        /// <summary>Whether every UI reference is assigned.</summary>
        public bool IsValid => m_Viewport != null && m_Content != null && m_MapImage != null && m_MapFitter != null;

        /// <summary>Size of the viewport in its local units.</summary>
        public Vector2 Size => m_Viewport != null ? m_Viewport.rect.size : Vector2.zero;

        /// <summary>Width divided by height of the map sprite, or of the viewport when there is no sprite.</summary>
        public float ContentAspect
        {
            get
            {
                Vector2 size = Size;
                float viewportAspect = size.y > 0f ? size.x / size.y : 1f;
                return MapUtil.GetAspect(m_MapImage != null ? m_MapImage.sprite : null, viewportAspect);
            }
        }

        public void SetSprite(Sprite sprite)
        {
            if (m_MapImage == null)
                return;

            m_MapImage.sprite = sprite;
            m_MapImage.enabled = sprite != null;
            if (m_MapFitter != null)
                m_MapFitter.aspectRatio = ContentAspect;
        }

        public void SetFitMode(MapFitMode fitMode)
        {
            if (m_MapFitter == null)
                return;

            AspectRatioFitter.AspectMode aspectMode = fitMode == MapFitMode.Fill
                ? AspectRatioFitter.AspectMode.EnvelopeParent
                : AspectRatioFitter.AspectMode.FitInParent;
            if (m_MapFitter.aspectMode != aspectMode)
                m_MapFitter.aspectMode = aspectMode;
        }

        /// <summary>Moves and scales the content to show the map at the given pan and zoom.</summary>
        public void Render(Vector2 pan, float zoom)
        {
            if (m_Content == null)
                return;

            // Only touch the transform when it changes, so an idle map does not rebuild the canvas.
            if (m_Content.anchoredPosition != pan)
                m_Content.anchoredPosition = pan;

            var scale = new Vector3(zoom, zoom, 1f);
            if (m_Content.localScale != scale)
                m_Content.localScale = scale;
        }

        public bool TryScreenToViewport(Vector2 screenPosition, out Vector2 viewportPosition) =>
            MapUtil.TryScreenToCenteredLocal(m_Viewport, screenPosition, out viewportPosition);
    }
}
