using NUnit.Framework;
using UnityEngine;

namespace POI.Tests
{
    public class POIUtilTests
    {
        static readonly Rect k_Area = new Rect(-400f, -300f, 800f, 600f);
        static readonly Rect k_IconExtents = new Rect(-32f, -32f, 64f, 64f);

        Camera _camera;
        RenderTexture _target;

        [TearDown]
        public void TearDown()
        {
            if (_camera != null)
                Object.DestroyImmediate(_camera.gameObject);
            if (_target != null)
                Object.DestroyImmediate(_target);
        }

        static void AssertApproximately(Vector2 expected, Vector2 actual, float tolerance = 1e-3f)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(tolerance), "x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(tolerance), "y");
        }

        static void AssertApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(1e-4f), "x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(1e-4f), "y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(1e-4f), "z");
        }

        // An 800x600 render target makes the camera's pixel rect independent of the Game view.
        Camera CreateCamera(bool orthographic = false)
        {
            _target = new RenderTexture(800, 600, 0);
            _camera = new GameObject("Camera").AddComponent<Camera>();
            _camera.targetTexture = _target;
            _camera.fieldOfView = 60f;
            _camera.orthographic = orthographic;
            return _camera;
        }

        [Test]
        public void GetPivotBounds_InsetsAreaByExtentsAndPadding()
        {
            Rect bounds = POIUtil.GetPivotBounds(k_Area, k_IconExtents, 10f, 20f, 30f, 40f);

            Assert.That(bounds.xMin, Is.EqualTo(-358f).Within(1e-3f));
            Assert.That(bounds.xMax, Is.EqualTo(348f).Within(1e-3f));
            Assert.That(bounds.yMin, Is.EqualTo(-238f).Within(1e-3f));
            Assert.That(bounds.yMax, Is.EqualTo(228f).Within(1e-3f));
        }

        [Test]
        public void GetPivotBounds_OffCenterPivot_KeepsWholeElementInside()
        {
            // Pivot at the left edge of a 100 wide element.
            var extents = new Rect(0f, -10f, 100f, 20f);

            Rect bounds = POIUtil.GetPivotBounds(k_Area, extents, 0f, 0f, 0f, 0f);

            Assert.That(bounds.xMin, Is.EqualTo(k_Area.xMin).Within(1e-3f));
            Assert.That(bounds.xMax, Is.EqualTo(k_Area.xMax - 100f).Within(1e-3f));
        }

        [Test]
        public void GetPivotBounds_ElementLargerThanArea_IsCentered()
        {
            var area = new Rect(-25f, -25f, 50f, 50f);

            Rect bounds = POIUtil.GetPivotBounds(area, k_IconExtents, new RectOffset(0, 0, 0, 0));

            AssertApproximately(Vector2.zero, bounds.min);
            AssertApproximately(Vector2.zero, bounds.max);
        }

        [Test]
        public void ClampTowards_PointInsideOrOnEdge_IsUnchanged()
        {
            AssertApproximately(new Vector2(10f, -20f), POIUtil.ClampTowards(k_Area, new Vector2(10f, -20f)));
            AssertApproximately(new Vector2(400f, 300f), POIUtil.ClampTowards(k_Area, new Vector2(400f, 300f)));
        }

        [Test]
        public void ClampTowards_PointOutside_LandsOnEdgeInSameDirection()
        {
            var point = new Vector2(1200f, 300f);

            Vector2 clamped = POIUtil.ClampTowards(k_Area, point);

            AssertApproximately(new Vector2(400f, 100f), clamped);
            AssertApproximately(point.normalized, clamped.normalized);
        }

        [Test]
        public void ClampTowards_PointBeyondCorner_LandsOnFirstEdgeAlongTheLine()
        {
            AssertApproximately(new Vector2(300f, 300f), POIUtil.ClampTowards(k_Area, new Vector2(900f, 900f)));
        }

        [Test]
        public void ClampTowards_OffCenterRect_MeasuresFromItsCenter()
        {
            var rect = new Rect(100f, 0f, 200f, 100f);

            AssertApproximately(new Vector2(300f, 50f), POIUtil.ClampTowards(rect, new Vector2(900f, 50f)));
        }

        [Test]
        public void GetHeading_FlattensForward()
        {
            AssertApproximately(Vector3.right, POIUtil.GetHeading(new Vector3(1f, 1f, 0f), Vector3.up, Vector3.up));
        }

        [Test]
        public void GetHeading_LookingStraightDownOrUp_UsesTheWayTheViewFaced()
        {
            // Pitched down or up from facing +Z, the top of the view points forward or back respectively.
            AssertApproximately(Vector3.forward, POIUtil.GetHeading(Vector3.down, Vector3.forward, Vector3.up));
            AssertApproximately(Vector3.forward, POIUtil.GetHeading(Vector3.up, Vector3.back, Vector3.up));
        }

        [Test]
        public void GetBearing_IsPositiveToTheRightAndNegativeToTheLeft()
        {
            Assert.That(POIUtil.GetBearing(Vector3.forward, Vector3.right, Vector3.up), Is.EqualTo(90f).Within(1e-3f));
            Assert.That(POIUtil.GetBearing(Vector3.forward, Vector3.left, Vector3.up), Is.EqualTo(-90f).Within(1e-3f));
            Assert.That(Mathf.Abs(POIUtil.GetBearing(Vector3.forward, Vector3.back, Vector3.up)), Is.EqualTo(180f).Within(1e-3f));
        }

        [Test]
        public void GetBearing_IgnoresHeightAndIsZeroStraightUp()
        {
            Assert.That(POIUtil.GetBearing(Vector3.forward, new Vector3(1f, 50f, 1f), Vector3.up), Is.EqualTo(45f).Within(1e-3f));
            Assert.That(POIUtil.GetBearing(Vector3.forward, Vector3.up, Vector3.up), Is.EqualTo(0f));
        }

        [Test]
        public void BearingToOffset_MapsFieldOfViewOntoWidth()
        {
            Assert.That(POIUtil.BearingToOffset(45f, 180f, 900f), Is.EqualTo(225f).Within(1e-3f));
            Assert.That(POIUtil.BearingToOffset(-90f, 180f, 900f), Is.EqualTo(-450f).Within(1e-3f));
            Assert.That(POIUtil.BearingToOffset(30f, 0f, 900f), Is.EqualTo(0f));
        }

        [Test]
        public void GetScreenTarget_PointInFront_ProjectsOntoScreen()
        {
            Camera camera = CreateCamera();

            AssertApproximately(new Vector2(400f, 300f), POIUtil.GetScreenTarget(camera, new Vector3(0f, 0f, 10f)), 0.05f);
        }

        [Test]
        public void GetScreenTarget_PointBehindToTheRight_IsFarOffTheRightEdge()
        {
            Camera camera = CreateCamera();

            Vector2 target = POIUtil.GetScreenTarget(camera, new Vector3(5f, 0f, -10f));

            // Not mirrored to the left, as a raw projection of a point behind the camera would be.
            Assert.That(target.x, Is.GreaterThan(800f * 10f));
            Assert.That(target.y, Is.EqualTo(300f).Within(0.05f));
        }

        [Test]
        public void GetScreenTarget_PointStraightBehind_IsFarBelowTheScreen()
        {
            Camera camera = CreateCamera();

            Vector2 target = POIUtil.GetScreenTarget(camera, new Vector3(0f, 0f, -10f));

            Assert.That(target.y, Is.LessThan(-600f * 10f));
            Assert.That(target.x, Is.EqualTo(400f).Within(0.05f));
        }

        [Test]
        public void GetScreenTarget_OrthographicCamera_ProjectsPointsBehindItToo()
        {
            Camera camera = CreateCamera(orthographic: true);
            Vector3 point = new Vector3(1f, 1f, -5f);

            AssertApproximately((Vector2)camera.WorldToScreenPoint(point), POIUtil.GetScreenTarget(camera, point));
        }

        [Test]
        public void GetBoundsAroundPivot_IncludesActiveChildrenAndScale()
        {
            var icon = new GameObject("Icon", typeof(RectTransform));
            try
            {
                var iconTransform = (RectTransform)icon.transform;
                iconTransform.sizeDelta = new Vector2(64f, 64f);
                iconTransform.localScale = new Vector3(2f, 2f, 1f);

                var label = (RectTransform)new GameObject("Label", typeof(RectTransform)).transform;
                label.SetParent(iconTransform, false);
                label.sizeDelta = new Vector2(120f, 20f);
                label.anchoredPosition = new Vector2(0f, -42f);

                var hidden = new GameObject("Hidden", typeof(RectTransform));
                hidden.transform.SetParent(iconTransform, false);
                ((RectTransform)hidden.transform).sizeDelta = new Vector2(1000f, 1000f);
                hidden.SetActive(false);

                Rect bounds = POIUtil.GetBoundsAroundPivot(iconTransform);

                AssertApproximately(new Vector2(-120f, -104f), bounds.min);
                AssertApproximately(new Vector2(120f, 64f), bounds.max);
            }
            finally
            {
                Object.DestroyImmediate(icon);
            }
        }

        [Test]
        public void IsValidDistanceFormat_AcceptsFormatsWithOneArgument()
        {
            Assert.That(POIUtil.IsValidDistanceFormat(POIUtil.DefaultDistanceFormat), Is.True);
            Assert.That(POIUtil.IsValidDistanceFormat("{0:N0}m"), Is.True);
            Assert.That(POIUtil.IsValidDistanceFormat(string.Empty), Is.True);
            Assert.That(POIUtil.IsValidDistanceFormat("{1} m"), Is.False);
            Assert.That(POIUtil.IsValidDistanceFormat("{0 m"), Is.False);
            Assert.That(POIUtil.IsValidDistanceFormat(null), Is.False);
        }
    }
}
