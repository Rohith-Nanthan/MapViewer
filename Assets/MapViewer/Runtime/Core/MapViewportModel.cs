using UnityEngine;

namespace Maps
{
    /// <summary>
    /// Pan and zoom state of a map seen through a rectangular viewport, plus the constraints that keep
    /// the map in view. It has no scene or UI dependencies, so any presentation layer can drive it.
    /// </summary>
    /// <remarks>
    /// Positions are in viewport space: the viewport's local units, with the origin at its center.
    /// Normalized map points run from (0, 0) at the bottom-left of the map to (1, 1) at its top-right.
    /// </remarks>
    public sealed class MapViewportModel
    {
        /// <summary>Lowest zoom limit accepted, so the map can never collapse to nothing.</summary>
        public const float MinimumZoom = 0.01f;

        static readonly Vector2 MapCenter = new Vector2(0.5f, 0.5f);

        Vector2 _viewportSize;
        float _contentAspect = 1f;
        MapFitMode _fitMode = MapFitMode.Fit;

        /// <summary>Size of the viewport the map is seen through.</summary>
        public Vector2 ViewportSize => _viewportSize;

        /// <summary>Width of the map divided by its height.</summary>
        public float ContentAspect => _contentAspect;

        /// <summary>How the map is sized inside the viewport at a zoom of 1.</summary>
        public MapFitMode FitMode => _fitMode;

        /// <summary>Size of the map at a zoom of 1.</summary>
        public Vector2 BaseContentSize { get; private set; }

        /// <summary>Size of the map at the current zoom.</summary>
        public Vector2 ContentSize => BaseContentSize * Zoom;

        /// <summary>Current zoom, as a multiple of <see cref="BaseContentSize"/>.</summary>
        public float Zoom { get; private set; } = 1f;

        /// <summary>Lowest zoom the view can reach.</summary>
        public float MinZoom { get; private set; } = 1f;

        /// <summary>Highest zoom the view can reach.</summary>
        public float MaxZoom { get; private set; } = 4f;

        /// <summary>Offset of the map's center from the viewport's center.</summary>
        public Vector2 Pan { get; private set; }

        public void SetViewportSize(Vector2 size)
        {
            size = Vector2.Max(size, Vector2.zero);
            if (size == _viewportSize)
                return;

            _viewportSize = size;
            UpdateBaseContentSize();
        }

        public void SetContentAspect(float aspect)
        {
            if (!(aspect > 0f) || float.IsInfinity(aspect))
                aspect = 1f;

            if (aspect == _contentAspect)
                return;

            _contentAspect = aspect;
            UpdateBaseContentSize();
        }

        public void SetFitMode(MapFitMode fitMode)
        {
            if (fitMode == _fitMode)
                return;

            _fitMode = fitMode;
            UpdateBaseContentSize();
        }

        /// <summary>Sets the zoom range, adjusting the current zoom if it falls outside of it.</summary>
        public void SetZoomLimits(float min, float max)
        {
            min = Mathf.Max(min, MinimumZoom);
            max = Mathf.Max(max, min);
            if (min == MinZoom && max == MaxZoom)
                return;

            MinZoom = min;
            MaxZoom = max;
            SetZoom(Zoom, Vector2.zero);
        }

        public float ClampZoom(float zoom) => Mathf.Clamp(zoom, MinZoom, MaxZoom);

        /// <summary>Sets the zoom while keeping the map point under <paramref name="pivot"/> in place.</summary>
        /// <param name="zoom">Requested zoom; clamped to the zoom limits.</param>
        /// <param name="pivot">Point in viewport space to zoom around.</param>
        public void SetZoom(float zoom, Vector2 pivot)
        {
            float clamped = ClampZoom(zoom);
            float ratio = clamped / Zoom;
            Zoom = clamped;
            Pan = ClampPan(pivot - (pivot - Pan) * ratio);
        }

        /// <summary>Moves the map by <paramref name="delta"/>, stopping at its edges.</summary>
        public void PanBy(Vector2 delta) => Pan = ClampPan(Pan + delta);

        /// <summary>Brings a map point as close to the viewport center as the map's edges allow.</summary>
        public void CenterOn(Vector2 normalizedPoint) =>
            Pan = ClampPan(-Vector2.Scale(normalizedPoint - MapCenter, ContentSize));

        /// <summary>Converts a normalized map point to viewport space.</summary>
        public Vector2 NormalizedToViewport(Vector2 normalizedPoint) =>
            Pan + Vector2.Scale(normalizedPoint - MapCenter, ContentSize);

        /// <summary>Converts a point in viewport space to a normalized map point.</summary>
        public Vector2 ViewportToNormalized(Vector2 viewportPoint)
        {
            Vector2 size = ContentSize;
            if (size.x <= 0f || size.y <= 0f)
                return MapCenter;

            Vector2 offset = viewportPoint - Pan;
            return new Vector2(offset.x / size.x, offset.y / size.y) + MapCenter;
        }

        void UpdateBaseContentSize()
        {
            Vector2 previous = BaseContentSize;
            BaseContentSize = CalculateBaseContentSize(_viewportSize, _contentAspect, _fitMode);

            // Rescale the offset so the same map point stays centered, e.g. across a resolution change.
            Vector2 pan = Pan;
            pan.x = previous.x > 0f ? pan.x * BaseContentSize.x / previous.x : 0f;
            pan.y = previous.y > 0f ? pan.y * BaseContentSize.y / previous.y : 0f;
            Pan = ClampPan(pan);
        }

        Vector2 ClampPan(Vector2 pan)
        {
            Vector2 overflow = (ContentSize - _viewportSize) * 0.5f;
            return new Vector2(ClampAxis(pan.x, overflow.x), ClampAxis(pan.y, overflow.y));
        }

        // A map smaller than the viewport along an axis stays centered on that axis.
        static float ClampAxis(float value, float overflow) =>
            overflow > 0f ? Mathf.Clamp(value, -overflow, overflow) : 0f;

        static Vector2 CalculateBaseContentSize(Vector2 viewportSize, float contentAspect, MapFitMode fitMode)
        {
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
                return Vector2.zero;

            float viewportAspect = viewportSize.x / viewportSize.y;
            bool matchWidth = contentAspect > viewportAspect == (fitMode == MapFitMode.Fit);
            return matchWidth
                ? new Vector2(viewportSize.x, viewportSize.x / contentAspect)
                : new Vector2(viewportSize.y * contentAspect, viewportSize.y);
        }
    }
}
