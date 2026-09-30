using UnityEngine;
using UnityEngine.UI;

namespace Maps
{
    /// <summary>
    /// uGUI presentation of the map. Sits on the masked rectangle the map is seen through, shows the sprite
    /// at its aspect ratio and moves and scales the content to render pan and zoom.
    /// </summary>
    [AddComponentMenu("Map Viewer/Map Viewport")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MapViewport : MonoBehaviour, IMapViewportSpace
    {
        [Tooltip("Child stretched over the viewport. It is moved to pan and scaled to zoom.")]
        [SerializeField] RectTransform content;

        [Tooltip("Image inside the content that displays the map sprite.")]
        [SerializeField] Image mapImage;

        [Tooltip("Keeps the map image at the sprite's aspect ratio inside the content.")]
        [SerializeField] AspectRatioFitter mapFitter;

        RectTransform _rectTransform;

        RectTransform RectTransform =>
            _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;

        /// <summary>Size of the viewport in its local units.</summary>
        public Vector2 Size => RectTransform.rect.size;

        /// <summary>Width divided by height of the map sprite, or of the viewport when there is no sprite.</summary>
        public float ContentAspect
        {
            get
            {
                Vector2 size = Size;
                float viewportAspect = size.y > 0f ? size.x / size.y : 1f;
                return MapUtil.GetAspect(mapImage != null ? mapImage.sprite : null, viewportAspect);
            }
        }

        public void SetSprite(Sprite sprite)
        {
            if (mapImage == null)
                return;

            mapImage.sprite = sprite;
            mapImage.enabled = sprite != null;
            if (mapFitter != null)
                mapFitter.aspectRatio = ContentAspect;
        }

        public void SetFitMode(MapFitMode fitMode)
        {
            if (mapFitter == null)
                return;

            AspectRatioFitter.AspectMode aspectMode = fitMode == MapFitMode.Fill
                ? AspectRatioFitter.AspectMode.EnvelopeParent
                : AspectRatioFitter.AspectMode.FitInParent;
            if (mapFitter.aspectMode != aspectMode)
                mapFitter.aspectMode = aspectMode;
        }

        /// <summary>Moves and scales the content to show the map at the given pan and zoom.</summary>
        public void Render(Vector2 pan, float zoom)
        {
            if (content == null)
                return;

            // Only touch the transform when it changes, so an idle map does not rebuild the canvas.
            if (content.anchoredPosition != pan)
                content.anchoredPosition = pan;

            var scale = new Vector3(zoom, zoom, 1f);
            if (content.localScale != scale)
                content.localScale = scale;
        }

        public bool TryScreenToViewport(Vector2 screenPosition, out Vector2 viewportPosition) =>
            MapUtil.TryScreenToCenteredLocal(RectTransform, screenPosition, out viewportPosition);
    }
}
