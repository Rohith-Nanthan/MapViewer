using UnityEngine;

namespace POI
{
    public static class POIMath
    {
        // Nearer the camera plane than this, or behind it, a perspective projection is unusable.
        const float k_MinViewDepth = 1e-3f;

        // How far off screen, in screen sizes, a point without a projection is placed, so its direction barely
        // depends on where in the viewport it is seen from.
        const float k_OffScreenDistance = 100f;

        const float k_SqrEpsilon = 1e-10f;

        // A point level with or behind a perspective camera maps far off screen in its direction, instead of to
        // Unity's projection, which mirrors it to the opposite side.
        public static Vector2 ProjectToScreen(Camera camera, Vector3 worldPosition)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(worldPosition);
            if (camera.orthographic || screenPoint.z > k_MinViewDepth)
                return screenPoint;

            Transform cameraTransform = camera.transform;
            Vector3 viewDirection = cameraTransform.InverseTransformDirection(worldPosition - cameraTransform.position);
            var direction = new Vector2(viewDirection.x, viewDirection.y);

            // Straight behind has no side to point to, so point down, past the player's back.
            if (direction.sqrMagnitude < k_SqrEpsilon)
                direction = Vector2.down;

            Rect pixelRect = camera.pixelRect;
            return pixelRect.center + direction.normalized * ((pixelRect.width + pixelRect.height) * k_OffScreenDistance);
        }

        // Includes active children and scale, in the parent's units; ignores rotation.
        public static Rect GetBoundsAroundPivot(RectTransform element)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(element);
            Vector3 scale = element.localScale;
            Vector2 a = Vector2.Scale(bounds.min, scale);
            Vector2 b = Vector2.Scale(bounds.max, scale);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        // Where a marker's pivot can go so the whole marker stays inside the container, the padding away from its
        // edges. On an axis where the marker does not fit, the area collapses to its middle to keep it centered.
        public static Rect GetAllowedPivotArea(Rect container, Rect markerBounds, EdgePadding padding)
        {
            float xMin = container.xMin + padding.Left - markerBounds.xMin;
            float xMax = container.xMax - padding.Right - markerBounds.xMax;
            float yMin = container.yMin + padding.Bottom - markerBounds.yMin;
            float yMax = container.yMax - padding.Top - markerBounds.yMax;

            if (xMin > xMax)
                xMin = xMax = (xMin + xMax) * 0.5f;
            if (yMin > yMax)
                yMin = yMax = (yMin + yMax) * 0.5f;

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        // A point outside the rect moves onto its edge along the line to the rect's center, keeping its direction.
        public static Vector2 ClampTowardCenter(Rect rect, Vector2 point)
        {
            if (ContainsInclusive(rect, point))
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

        // Looking straight down or up, the top of the view gives the heading, so it stays continuous.
        public static Vector3 GetHeading(Vector3 forward, Vector3 up, Vector3 worldUp)
        {
            Vector3 heading = Vector3.ProjectOnPlane(forward, worldUp);
            if (heading.sqrMagnitude > k_SqrEpsilon)
                return heading.normalized;

            heading = Vector3.ProjectOnPlane(Vector3.Dot(forward, worldUp) < 0f ? up : -up, worldUp);
            return heading.sqrMagnitude > k_SqrEpsilon ? heading.normalized : Vector3.forward;
        }

        // Degrees from -180 to 180, positive to the right. A direction straight up or down has a bearing of 0.
        public static float GetBearing(Vector3 heading, Vector3 direction, Vector3 worldUp)
        {
            Vector3 flatDirection = Vector3.ProjectOnPlane(direction, worldUp);
            if (flatDirection.sqrMagnitude <= k_SqrEpsilon)
                return 0f;

            return Vector3.SignedAngle(Vector3.ProjectOnPlane(heading, worldUp), flatDirection, worldUp);
        }

        public static float BearingToOffset(float bearing, float degreesAcrossWidth, float width) =>
            degreesAcrossWidth > 0f ? bearing / degreesAcrossWidth * width : 0f;

        static bool ContainsInclusive(Rect rect, Vector2 point) =>
            point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax;
    }
}
