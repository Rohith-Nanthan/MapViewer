using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace POI
{
    /// <summary>
    /// Stateless math and UI helpers behind the POI markers: projecting world points onto the screen,
    /// keeping UI elements inside a rectangle and measuring compass bearings. New kinds of markers, such as a
    /// minimap, can reuse them directly.
    /// </summary>
    /// <remarks>
    /// Local positions are in a RectTransform's own units, with the origin at its pivot. Bearings are in
    /// degrees around the world up axis, positive to the right (clockwise seen from above).
    /// </remarks>
    public static class POIUtil
    {
        /// <summary>Distance text used when none is set. {0} is the distance in whole meters.</summary>
        public const string DefaultDistanceFormat = "{0} m";

        // Points nearer the camera plane than this, or behind it, have no usable perspective projection.
        const float k_MinViewDepth = 1e-3f;

        // How far off screen, in screen sizes, a point with no projection is placed. Far enough that its
        // direction barely depends on where in the viewport it is seen from.
        const float k_OffScreenDistance = 100f;

        const float k_SqrEpsilon = 1e-10f;

        /// <summary>
        /// Screen position of a world point, in pixels. A point level with or behind a perspective camera has no
        /// projection, so a point far off screen in its direction is returned instead: that is where an edge
        /// marker should sit to point at it.
        /// </summary>
        public static Vector2 GetScreenTarget(Camera camera, Vector3 worldPosition)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(worldPosition);
            if (camera.orthographic || screenPoint.z > k_MinViewDepth)
                return screenPoint;

            Transform cameraTransform = camera.transform;
            Vector3 viewDirection = cameraTransform.InverseTransformDirection(worldPosition - cameraTransform.position);
            var direction = new Vector2(viewDirection.x, viewDirection.y);

            // Straight behind there is no side to point to, so point down, past the player's back.
            if (direction.sqrMagnitude < k_SqrEpsilon)
                direction = Vector2.down;

            Rect pixelRect = camera.pixelRect;
            return pixelRect.center + direction.normalized * ((pixelRect.width + pixelRect.height) * k_OffScreenDistance);
        }

        /// <summary>Camera that renders the canvas a RectTransform is on, or null for Screen Space - Overlay.</summary>
        public static Camera GetCanvasCamera(RectTransform rectTransform)
        {
            Canvas canvas = rectTransform != null ? rectTransform.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                return null;

            Canvas rootCanvas = canvas.rootCanvas;
            return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        }

        /// <summary>
        /// Bounds of a UI element and its active children relative to the element's pivot, in its parent's units.
        /// The element's rotation is ignored.
        /// </summary>
        public static Rect GetBoundsAroundPivot(RectTransform element)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(element);
            Vector3 scale = element.localScale;
            Vector2 a = Vector2.Scale(bounds.min, scale);
            Vector2 b = Vector2.Scale(bounds.max, scale);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>
        /// Range the pivot of an element can move in so the whole element stays inside <paramref name="area"/>, at
        /// least the padding away from its edges. Along an axis where the element does not fit, the range
        /// collapses to its middle, which keeps the element centered rather than cut on one side.
        /// </summary>
        /// <param name="extents">The element's bounds around its pivot, e.g. from <see cref="GetBoundsAroundPivot"/>.</param>
        public static Rect GetPivotBounds(Rect area, Rect extents, float left, float right, float bottom, float top)
        {
            float xMin = area.xMin + left - extents.xMin;
            float xMax = area.xMax - right - extents.xMax;
            float yMin = area.yMin + bottom - extents.yMin;
            float yMax = area.yMax - top - extents.yMax;

            if (xMin > xMax)
                xMin = xMax = (xMin + xMax) * 0.5f;
            if (yMin > yMax)
                yMin = yMax = (yMin + yMax) * 0.5f;

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <inheritdoc cref="GetPivotBounds(Rect, Rect, float, float, float, float)"/>
        public static Rect GetPivotBounds(Rect area, Rect extents, EdgePadding padding) =>
            GetPivotBounds(area, extents, padding.Left, padding.Right, padding.Bottom, padding.Top);

        /// <summary>Whether a point lies inside a rectangle or on its edge.</summary>
        public static bool Contains(Rect rect, Vector2 point) =>
            point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax;

        /// <summary>
        /// Returns a point inside a rectangle unchanged, and moves a point outside it onto the rectangle's edge,
        /// along the line from the rectangle's center, so the result still lies in the point's direction.
        /// </summary>
        public static Vector2 ClampTowards(Rect rect, Vector2 point)
        {
            if (Contains(rect, point))
                return point;

            Vector2 center = rect.center;
            Vector2 offset = point - center;
            Vector2 halfSize = rect.size * 0.5f;

            float scale = 1f;
            if (Mathf.Abs(offset.x) > halfSize.x)
                scale = halfSize.x / Mathf.Abs(offset.x);
            if (Mathf.Abs(offset.y) * scale > halfSize.y)
                scale = halfSize.y / Mathf.Abs(offset.y);

            return center + offset * scale;
        }

        /// <summary>
        /// Horizontal direction a camera faces, which is the center of a compass. Looking straight down the top
        /// of the view points ahead, and looking straight up it points behind, so the heading stays continuous.
        /// </summary>
        public static Vector3 GetHeading(Vector3 forward, Vector3 up, Vector3 worldUp)
        {
            Vector3 heading = Vector3.ProjectOnPlane(forward, worldUp);
            if (heading.sqrMagnitude > k_SqrEpsilon)
                return heading.normalized;

            heading = Vector3.ProjectOnPlane(Vector3.Dot(forward, worldUp) < 0f ? up : -up, worldUp);
            return heading.sqrMagnitude > k_SqrEpsilon ? heading.normalized : Vector3.forward;
        }

        /// <summary>
        /// Horizontal angle in degrees from a heading to a direction, between -180 and 180: positive to the right,
        /// negative to the left. A direction straight up or down has no bearing and returns 0.
        /// </summary>
        public static float GetBearing(Vector3 heading, Vector3 direction, Vector3 worldUp)
        {
            Vector3 flatDirection = Vector3.ProjectOnPlane(direction, worldUp);
            if (flatDirection.sqrMagnitude <= k_SqrEpsilon)
                return 0f;

            return Vector3.SignedAngle(Vector3.ProjectOnPlane(heading, worldUp), flatDirection, worldUp);
        }

        /// <summary>
        /// Horizontal offset from the center of a compass of the given width for a bearing, when the width spans
        /// <paramref name="fieldOfView"/> degrees. It is not clamped, so bearings outside the field of view land
        /// beyond the edges.
        /// </summary>
        public static float BearingToOffset(float bearing, float fieldOfView, float width) =>
            fieldOfView > 0f ? bearing / fieldOfView * width : 0f;

        /// <summary>Whether a distance format can be used with <see cref="string.Format(string, object)"/>.</summary>
        public static bool IsValidDistanceFormat(string format)
        {
            if (format == null)
                return false;

            try
            {
                string.Format(format, 0);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // Markers are destroyed both in Play Mode and by Edit Mode tests.
        internal static void DestroyObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
