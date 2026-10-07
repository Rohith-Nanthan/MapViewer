using NUnit.Framework;
using UnityEngine;

namespace POI.Tests
{
    public class POIMathTests
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
        public void GetAllowedPivotArea_InsetsEachEdgeByMarkerBoundsAndItsOwnPadding()
        {
            var padding = new EdgePadding(left: 10f, right: 20f, top: 40f, bottom: 30f);

            Rect area = POIMath.GetAllowedPivotArea(k_Area, k_IconExtents, padding);

            Assert.That(area.xMin, Is.EqualTo(-358f).Within(1e-3f));
            Assert.That(area.xMax, Is.EqualTo(348f).Within(1e-3f));
            Assert.That(area.yMin, Is.EqualTo(-238f).Within(1e-3f));
            Assert.That(area.yMax, Is.EqualTo(228f).Within(1e-3f));
        }

        [Test]
        public void EdgePadding_TreatsNegativeValuesAsZero()
        {
            var padding = new EdgePadding(-5f, 3f, -1f, 2f);

            Assert.That(padding, Is.EqualTo(new EdgePadding(0f, 3f, 0f, 2f)));
        }

        [Test]
        public void GetAllowedPivotArea_OffCenterPivot_KeepsWholeElementInside()
        {
            // Pivot at the left edge of a 100 wide element.
            var extents = new Rect(0f, -10f, 100f, 20f);

            Rect bounds = POIMath.GetAllowedPivotArea(k_Area, extents, new EdgePadding(0f, 0f, 0f, 0f));

            Assert.That(bounds.xMin, Is.EqualTo(k_Area.xMin).Within(1e-3f));
            Assert.That(bounds.xMax, Is.EqualTo(k_Area.xMax - 100f).Within(1e-3f));
        }

        [Test]
        public void GetAllowedPivotArea_ElementLargerThanArea_IsCentered()
        {
            var area = new Rect(-25f, -25f, 50f, 50f);

            Rect bounds = POIMath.GetAllowedPivotArea(area, k_IconExtents, new EdgePadding(0f));

            AssertApproximately(Vector2.zero, bounds.min);
            AssertApproximately(Vector2.zero, bounds.max);
        }

        [Test]
        public void ClampTowardCenter_PointInsideOrOnEdge_IsUnchanged()
        {
            AssertApproximately(new Vector2(10f, -20f), POIMath.ClampTowardCenter(k_Area, new Vector2(10f, -20f)));
            AssertApproximately(new Vector2(400f, 300f), POIMath.ClampTowardCenter(k_Area, new Vector2(400f, 300f)));
        }

        [Test]
        public void ClampTowardCenter_PointOutside_LandsOnEdgeInSameDirection()
        {
            var point = new Vector2(1200f, 300f);

            Vector2 clamped = POIMath.ClampTowardCenter(k_Area, point);

            AssertApproximately(new Vector2(400f, 100f), clamped);
            AssertApproximately(point.normalized, clamped.normalized);
        }

        [Test]
        public void ClampTowardCenter_PointBeyondCorner_LandsOnFirstEdgeAlongTheLine()
        {
            AssertApproximately(new Vector2(300f, 300f), POIMath.ClampTowardCenter(k_Area, new Vector2(900f, 900f)));
        }

        [Test]
        public void ClampTowardCenter_OffCenterRect_MeasuresFromItsCenter()
        {
            var rect = new Rect(100f, 0f, 200f, 100f);

            AssertApproximately(new Vector2(300f, 50f), POIMath.ClampTowardCenter(rect, new Vector2(900f, 50f)));
        }

        [Test]
        public void GetHeading_FlattensForward()
        {
            AssertApproximately(Vector3.right, POIMath.GetHeading(new Vector3(1f, 1f, 0f), Vector3.up, Vector3.up));
        }

        [Test]
        public void GetHeading_LookingStraightDownOrUp_UsesTheWayTheViewFaced()
        {
            // Pitched down or up from facing +Z, the top of the view points forward or back respectively.
            AssertApproximately(Vector3.forward, POIMath.GetHeading(Vector3.down, Vector3.forward, Vector3.up));
            AssertApproximately(Vector3.forward, POIMath.GetHeading(Vector3.up, Vector3.back, Vector3.up));
        }

        [Test]
        public void GetBearing_IsPositiveToTheRightAndNegativeToTheLeft()
        {
            Assert.That(POIMath.GetBearing(Vector3.forward, Vector3.right, Vector3.up), Is.EqualTo(90f).Within(1e-3f));
            Assert.That(POIMath.GetBearing(Vector3.forward, Vector3.left, Vector3.up), Is.EqualTo(-90f).Within(1e-3f));
            Assert.That(Mathf.Abs(POIMath.GetBearing(Vector3.forward, Vector3.back, Vector3.up)), Is.EqualTo(180f).Within(1e-3f));
        }

        [Test]
        public void GetBearing_IgnoresHeightAndIsZeroStraightUp()
        {
            Assert.That(POIMath.GetBearing(Vector3.forward, new Vector3(1f, 50f, 1f), Vector3.up), Is.EqualTo(45f).Within(1e-3f));
            Assert.That(POIMath.GetBearing(Vector3.forward, Vector3.up, Vector3.up), Is.EqualTo(0f));
        }

        [Test]
        public void BearingToOffset_MapsDegreesOntoWidth()
        {
            Assert.That(POIMath.BearingToOffset(45f, 180f, 900f), Is.EqualTo(225f).Within(1e-3f));
            Assert.That(POIMath.BearingToOffset(-90f, 180f, 900f), Is.EqualTo(-450f).Within(1e-3f));
            Assert.That(POIMath.BearingToOffset(30f, 0f, 900f), Is.EqualTo(0f));
        }

        [Test]
        public void ProjectToScreen_PointInFront_ProjectsOntoScreen()
        {
            Camera camera = CreateCamera();

            AssertApproximately(new Vector2(400f, 300f), POIMath.ProjectToScreen(camera, new Vector3(0f, 0f, 10f)), 0.05f);
        }

        [Test]
        public void ProjectToScreen_PointBehindToTheRight_IsFarOffTheRightEdge()
        {
            Camera camera = CreateCamera();

            Vector2 target = POIMath.ProjectToScreen(camera, new Vector3(5f, 0f, -10f));

            // Not mirrored to the left, as a raw projection of a point behind the camera would be.
            Assert.That(target.x, Is.GreaterThan(800f * 10f));
            Assert.That(target.y, Is.EqualTo(300f).Within(0.05f));
        }

        [Test]
        public void ProjectToScreen_PointStraightBehind_IsFarBelowTheScreen()
        {
            Camera camera = CreateCamera();

            Vector2 target = POIMath.ProjectToScreen(camera, new Vector3(0f, 0f, -10f));

            Assert.That(target.y, Is.LessThan(-600f * 10f));
            Assert.That(target.x, Is.EqualTo(400f).Within(0.05f));
        }

        [Test]
        public void ProjectToScreen_OrthographicCamera_ProjectsPointsBehindItToo()
        {
            Camera camera = CreateCamera(orthographic: true);
            Vector3 point = new Vector3(1f, 1f, -5f);

            AssertApproximately((Vector2)camera.WorldToScreenPoint(point), POIMath.ProjectToScreen(camera, point));
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

                Rect bounds = POIMath.GetBoundsAroundPivot(iconTransform);

                AssertApproximately(new Vector2(-120f, -104f), bounds.min);
                AssertApproximately(new Vector2(120f, 64f), bounds.max);
            }
            finally
            {
                Object.DestroyImmediate(icon);
            }
        }
    }
}
