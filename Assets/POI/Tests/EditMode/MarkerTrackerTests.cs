using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POI.Tests
{
    /// <summary>
    /// Drives the screen and compass trackers directly with an 800x600 camera. The viewports have no canvas, so
    /// they behave like Screen Space - Overlay: one unit is one screen pixel.
    /// </summary>
    public class MarkerTrackerTests
    {
        const int k_Padding = 16;
        const float k_CompassPadding = 8f;
        const float k_CompassMarkerHeight = 10f;

        Camera _camera;
        RenderTexture _target;
        GameObject _root;
        RectTransform _screenViewport;
        RectTransform _compassViewport;
        Texture2D _texture;
        Sprite _sprite;
        ScreenMarkerTracker _screen;
        CompassMarkerTracker _compass;

        [SetUp]
        public void SetUp()
        {
            _target = new RenderTexture(800, 600, 0);
            _camera = new GameObject("Camera").AddComponent<Camera>();
            _camera.targetTexture = _target;
            _camera.fieldOfView = 60f;

            _root = new GameObject("UI", typeof(RectTransform));
            _screenViewport = CreateRect("Screen Viewport", _root.transform, new Vector2(800f, 600f));
            _screenViewport.position = new Vector3(400f, 300f, 0f);
            _compassViewport = CreateRect("Compass Viewport", _root.transform, new Vector2(900f, 100f));
            _compassViewport.position = new Vector3(400f, 550f, 0f);

            RectTransform screenIcon = CreateIcon("Screen Marker", _screenViewport, new Vector2(64f, 64f));

            // A 48 unit icon with a wider 120 unit distance label underneath.
            RectTransform compassIcon = CreateIcon("Compass Marker", _compassViewport, new Vector2(48f, 48f));
            compassIcon.localPosition = new Vector3(0f, k_CompassMarkerHeight, 0f);
            RectTransform label = CreateRect("Distance", compassIcon, new Vector2(120f, 24f));
            label.anchoredPosition = new Vector2(0f, -36f);
            label.gameObject.AddComponent<TextMeshProUGUI>();

            // Added once the children exist, so Reset finds the icon and the label.
            var screenTemplate = screenIcon.gameObject.AddComponent<POI_Marker>();
            var compassTemplate = compassIcon.gameObject.AddComponent<POI_Marker>();

            _screen = new ScreenMarkerTracker(new ScreenMarkerSettings(_screenViewport, screenTemplate)
            {
                Padding = new EdgePadding(k_Padding),
            });
            _compass = new CompassMarkerTracker(new CompassMarkerSettings(_compassViewport, compassTemplate)
            {
                FieldOfView = 180f,
                Padding = k_CompassPadding,
            });

            _texture = new Texture2D(4, 4);
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
        }

        [TearDown]
        public void TearDown()
        {
            _screen.Dispose();
            _compass.Dispose();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_camera.gameObject);
            Object.DestroyImmediate(_target);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 size)
        {
            var rectTransform = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rectTransform.SetParent(parent, false);
            rectTransform.sizeDelta = size;
            return rectTransform;
        }

        static RectTransform CreateIcon(string name, Transform viewport, Vector2 size)
        {
            RectTransform icon = CreateRect(name, viewport, size);
            icon.gameObject.AddComponent<Image>();
            return icon;
        }

        FakePointOfInterest CreatePoint(Vector3 position) => new FakePointOfInterest { Icon = _sprite, Position = position };

        POI_Marker Place(MarkerTracker tracker, IPointOfInterest poi, Vector3 playerPosition = default)
        {
            tracker.Add(poi);
            tracker.Update(new POIViewContext(_camera, playerPosition));
            Assert.That(tracker.TryGetMarker(poi, out POI_Marker marker), Is.True);
            return marker;
        }

        static Vector2 LocalPosition(POI_Marker marker) => marker.transform.localPosition;

        static void AssertApproximately(Vector2 expected, Vector2 actual, float tolerance = 0.05f)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(tolerance), "x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(tolerance), "y");
        }

        static void AssertInside(RectTransform viewport, POI_Marker marker, float horizontalPadding, float verticalPadding)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, marker.transform);
            Rect area = viewport.rect;
            const float tolerance = 0.01f;
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(area.xMin + horizontalPadding - tolerance), "left");
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(area.xMax - horizontalPadding + tolerance), "right");
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(area.yMin + verticalPadding - tolerance), "bottom");
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(area.yMax - verticalPadding + tolerance), "top");
        }

        [Test]
        public void Screen_PointAhead_ShowsIconOverIt()
        {
            POI_Marker marker = Place(_screen, CreatePoint(new Vector3(0f, 0f, 10f)));

            Assert.That(marker.IsVisible, Is.True);
            Assert.That(marker.Icon.sprite, Is.SameAs(_sprite));
            AssertApproximately(Vector2.zero, LocalPosition(marker));
        }

        [Test]
        public void Screen_PointInView_FollowsItsProjection()
        {
            var position = new Vector3(2f, 1f, 10f);

            POI_Marker marker = Place(_screen, CreatePoint(position));

            Vector2 expected = (Vector2)_camera.WorldToScreenPoint(position) - new Vector2(400f, 300f);
            AssertApproximately(expected, LocalPosition(marker));
        }

        [Test]
        public void Screen_PointOutOfViewToTheRight_HugsTheRightEdge()
        {
            POI_Marker marker = Place(_screen, CreatePoint(new Vector3(100f, 0f, 10f)));

            AssertApproximately(new Vector2(400f - k_Padding - 32f, 0f), LocalPosition(marker));
        }

        [Test]
        public void Screen_PointBehindToTheLeft_HugsTheLeftEdge()
        {
            POI_Marker marker = Place(_screen, CreatePoint(new Vector3(-5f, 0f, -10f)));

            AssertApproximately(new Vector2(-400f + k_Padding + 32f, 0f), LocalPosition(marker));
        }

        [Test]
        public void Screen_MarkerStaysFullyInsideThePaddedViewportInEveryDirection()
        {
            for (int angle = 0; angle < 360; angle += 15)
            {
                for (float height = -20f; height <= 20f; height += 10f)
                {
                    Quaternion yaw = Quaternion.Euler(0f, angle, 0f);
                    POI_Marker marker = Place(_screen, CreatePoint(yaw * new Vector3(0f, height, 30f)));

                    AssertInside(_screenViewport, marker, k_Padding, k_Padding);
                }
            }
        }

        [Test]
        public void Screen_RemovedPoint_HidesItsMarker()
        {
            FakePointOfInterest poi = CreatePoint(new Vector3(0f, 0f, 10f));
            POI_Marker marker = Place(_screen, poi);

            _screen.Remove(poi);

            Assert.That(marker.IsVisible, Is.False);
            Assert.That(_screen.MarkerCount, Is.EqualTo(0));
        }

        [Test]
        public void Add_BeforeViewportIsAssigned_ShowsOnceItIs()
        {
            var settings = new ScreenMarkerSettings(null, _screen.Settings.Template);
            var tracker = new ScreenMarkerTracker(settings);
            try
            {
                FakePointOfInterest poi = CreatePoint(new Vector3(0f, 0f, 10f));
                tracker.Add(poi);
                tracker.Update(new POIViewContext(_camera, Vector3.zero));
                Assert.That(tracker.MarkerCount, Is.EqualTo(0));

                settings.Viewport = _screenViewport;
                tracker.Update(new POIViewContext(_camera, Vector3.zero));

                Assert.That(tracker.TryGetMarker(poi, out POI_Marker marker), Is.True);
                Assert.That(marker.IsVisible, Is.True);
            }
            finally
            {
                tracker.Dispose();
            }
        }

        [Test]
        public void Hide_HidesMarkersUntilTheNextUpdate()
        {
            POI_Marker marker = Place(_screen, CreatePoint(new Vector3(0f, 0f, 10f)));

            _screen.Hide();
            Assert.That(marker.IsVisible, Is.False);

            _screen.Update(new POIViewContext(_camera, Vector3.zero));
            Assert.That(marker.IsVisible, Is.True);
        }

        [Test]
        public void Compass_PointAhead_IsCenteredAtTheTemplateHeight()
        {
            POI_Marker marker = Place(_compass, CreatePoint(new Vector3(0f, 5f, 10f)));

            Assert.That(marker.IsVisible, Is.True);
            AssertApproximately(new Vector2(0f, k_CompassMarkerHeight), LocalPosition(marker));
        }

        [Test]
        public void Compass_PointAt45DegreesRight_MovesAQuarterOfTheWidth()
        {
            POI_Marker marker = Place(_compass, CreatePoint(new Vector3(10f, 0f, 10f)));

            // 45 of 180 degrees across 900 units.
            Assert.That(LocalPosition(marker).x, Is.EqualTo(225f).Within(0.05f));
        }

        [Test]
        public void Compass_IgnoresCameraPitchAndFollowsYaw()
        {
            FakePointOfInterest poi = CreatePoint(new Vector3(10f, 0f, 10f));
            _camera.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            POI_Marker marker = Place(_compass, poi);
            Assert.That(LocalPosition(marker).x, Is.EqualTo(225f).Within(0.05f));

            _camera.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            _compass.Update(new POIViewContext(_camera, Vector3.zero));
            Assert.That(LocalPosition(marker).x, Is.EqualTo(0f).Within(0.05f));
        }

        [Test]
        public void Compass_PointBehind_StopsAtTheEdgeWithTheLabelInside()
        {
            POI_Marker marker = Place(_compass, CreatePoint(new Vector3(-1f, 0f, -10f)));

            // The 120 unit label is wider than the icon, so it decides how close to the edge the marker gets.
            Assert.That(LocalPosition(marker).x, Is.EqualTo(-450f + k_CompassPadding + 60f).Within(0.05f));
            AssertInside(_compassViewport, marker, k_CompassPadding, 0f);
        }

        [Test]
        public void Compass_ShowsDistanceFromThePlayer()
        {
            POI_Marker marker = Place(_compass, CreatePoint(new Vector3(0f, 0f, 10f)), new Vector3(0f, 0f, -5f));

            Assert.That(marker.DistanceLabel.text, Is.EqualTo("15 m"));
        }
    }
}
