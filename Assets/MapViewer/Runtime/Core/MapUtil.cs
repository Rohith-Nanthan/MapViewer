using UnityEngine;

namespace Maps
{
    /// <summary>
    /// General-purpose map math and UI helpers. They are stateless, so new map features such as markers,
    /// a minimap or touch pinch can reuse them directly.
    /// </summary>
    /// <remarks>
    /// "Centered" coordinates are local to a rectangle and measured from its center. Normalized map points
    /// run from (0, 0) at the bottom-left of the map to (1, 1) at its top-right.
    /// </remarks>
    public static class MapUtil
    {
        /// <summary>Center of a map in normalized coordinates.</summary>
        public static readonly Vector2 NormalizedCenter = new Vector2(0.5f, 0.5f);

        /// <summary>Size of content with the given aspect ratio when fitted inside, or filling, a viewport.</summary>
        /// <returns>Zero when the viewport has no area or the aspect ratio is not positive.</returns>
        public static Vector2 FitSize(Vector2 viewportSize, float contentAspect, MapFitMode fitMode)
        {
            if (viewportSize.x <= 0f || viewportSize.y <= 0f || !(contentAspect > 0f))
                return Vector2.zero;

            float viewportAspect = viewportSize.x / viewportSize.y;
            bool matchWidth = contentAspect > viewportAspect == (fitMode == MapFitMode.Fit);
            return matchWidth
                ? new Vector2(viewportSize.x, viewportSize.x / contentAspect)
                : new Vector2(viewportSize.y * contentAspect, viewportSize.y);
        }

        /// <summary>
        /// Clamps the offset of content centered in a viewport so its edges never come inside the viewport.
        /// Along an axis where the content is smaller than the viewport, it is centered instead.
        /// </summary>
        public static Vector2 ClampOffset(Vector2 offset, Vector2 contentSize, Vector2 viewportSize)
        {
            Vector2 overflow = (contentSize - viewportSize) * 0.5f;
            return new Vector2(ClampAxis(offset.x, overflow.x), ClampAxis(offset.y, overflow.y));
        }

        /// <summary>
        /// Offset that keeps the content point under <paramref name="pivot"/> in place when the content is
        /// scaled by <paramref name="scaleRatio"/> (new scale divided by old scale).
        /// </summary>
        public static Vector2 ScaleOffsetAroundPivot(Vector2 offset, Vector2 pivot, float scaleRatio) =>
            pivot - (pivot - offset) * scaleRatio;

        /// <summary>Converts a normalized content point to the space the content is offset in.</summary>
        public static Vector2 NormalizedToLocal(Vector2 normalizedPoint, Vector2 contentOffset, Vector2 contentSize) =>
            contentOffset + Vector2.Scale(normalizedPoint - NormalizedCenter, contentSize);

        /// <summary>Converts a point in the space the content is offset in to a normalized content point.</summary>
        public static Vector2 LocalToNormalized(Vector2 localPoint, Vector2 contentOffset, Vector2 contentSize)
        {
            if (contentSize.x <= 0f || contentSize.y <= 0f)
                return NormalizedCenter;

            Vector2 offset = localPoint - contentOffset;
            return new Vector2(offset.x / contentSize.x, offset.y / contentSize.y) + NormalizedCenter;
        }

        /// <summary>Whether a point lies inside a rectangle of the given size centered on the origin.</summary>
        public static bool ContainsCentered(Vector2 size, Vector2 point) =>
            Mathf.Abs(point.x) <= size.x * 0.5f && Mathf.Abs(point.y) <= size.y * 0.5f;

        /// <summary>Clamps a point into a rectangle of the given size centered on the origin.</summary>
        public static Vector2 ClampCentered(Vector2 size, Vector2 point)
        {
            Vector2 halfSize = size * 0.5f;
            return Vector2.Max(-halfSize, Vector2.Min(halfSize, point));
        }

        /// <summary>
        /// Blend factor for frame-rate independent exponential smoothing: it covers about 63% of the remaining
        /// distance every <paramref name="smoothTime"/> seconds, and snaps (returns 1) when smoothTime is 0.
        /// </summary>
        public static float SmoothingFactor(float smoothTime, float deltaTime) =>
            smoothTime > 0f ? 1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) / smoothTime) : 1f;

        /// <summary>Interpolates between two positive values in log space, so zooming in and out feel symmetrical.</summary>
        public static float LerpLog(float from, float to, float t) =>
            Mathf.Exp(Mathf.Lerp(Mathf.Log(from), Mathf.Log(to), t));

        /// <summary>Width divided by height of a sprite, or <paramref name="fallback"/> when there is none.</summary>
        public static float GetAspect(Sprite sprite, float fallback = 1f)
        {
            if (sprite == null)
                return fallback;

            Rect rect = sprite.rect;
            return rect.height > 0f ? rect.width / rect.height : fallback;
        }

        /// <summary>Converts a screen position to centered coordinates of a RectTransform, for any canvas render mode.</summary>
        /// <returns>False when the position cannot be projected onto the rectangle's plane.</returns>
        public static bool TryScreenToCenteredLocal(RectTransform rectTransform, Vector2 screenPosition, out Vector2 localPosition)
        {
            if (rectTransform == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPosition, GetEventCamera(rectTransform), out Vector2 pivotLocalPosition))
            {
                localPosition = default;
                return false;
            }

            localPosition = pivotLocalPosition - rectTransform.rect.center;
            return true;
        }

        /// <summary>Camera that renders the canvas a RectTransform is on, or null for Screen Space - Overlay.</summary>
        public static Camera GetEventCamera(RectTransform rectTransform)
        {
            Canvas canvas = rectTransform != null ? rectTransform.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                return null;

            Canvas rootCanvas = canvas.rootCanvas;
            return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        }

        // Content smaller than the viewport along an axis stays centered on that axis.
        static float ClampAxis(float value, float overflow) =>
            overflow > 0f ? Mathf.Clamp(value, -overflow, overflow) : 0f;
    }
}
