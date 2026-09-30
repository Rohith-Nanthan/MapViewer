using NUnit.Framework;
using UnityEngine;

namespace Maps.Tests
{
    public class MapNavigationControllerTests
    {
        // The fake screen matches the 1600 x 900 viewport, so screen (800, 450) is the viewport center.
        static readonly Vector2 k_ScreenCenter = new Vector2(800f, 450f);

        MapPanSettings _panSettings;
        MapZoomSettings _zoomSettings;
        MapViewportModel _model;
        MapNavigationController _controller;

        sealed class FakeViewportSpace : IMapViewportSpace
        {
            public bool TryScreenToViewport(Vector2 screenPosition, out Vector2 viewportPosition)
            {
                viewportPosition = screenPosition - k_ScreenCenter;
                return true;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _panSettings = new MapPanSettings { SmoothTime = 0f };
            _zoomSettings = new MapZoomSettings { SmoothTime = 0f };

            _model = new MapViewportModel();
            _model.SetZoomLimits(1f, 4f);
            _model.SetContentAspect(2f);
            _model.SetViewportSize(new Vector2(1600f, 900f));

            _controller = new MapNavigationController(_model, new FakeViewportSpace(), _panSettings, _zoomSettings);
        }

        static MapInputFrame PointerAt(Vector2 screenPosition) =>
            new MapInputFrame { HasPointer = true, PointerPosition = screenPosition };

        void ZoomModelTo(float zoom)
        {
            _model.SetZoom(zoom, Vector2.zero);
            _controller.Stop();
        }

        static void AssertApproximately(Vector2 expected, Vector2 actual, float tolerance = 1e-2f)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(tolerance), "x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(tolerance), "y");
        }

        [Test]
        public void DirectionalPan_MovesViewWithStick()
        {
            ZoomModelTo(4f);
            _panSettings.Speed = 1f;

            _controller.Tick(new MapInputFrame { Pan = Vector2.right }, 0.1f);

            // One viewport length (900) per second for 0.1s; the map moves opposite to the view.
            AssertApproximately(new Vector2(-90f, 0f), _model.Pan);
        }

        [Test]
        public void DirectionalPan_Inverted_MovesMapWithStick()
        {
            ZoomModelTo(4f);
            _panSettings.Speed = 1f;
            _panSettings.Invert = true;

            _controller.Tick(new MapInputFrame { Pan = Vector2.up }, 0.1f);

            AssertApproximately(new Vector2(0f, 90f), _model.Pan);
        }

        [Test]
        public void DirectionalPan_WithSmoothTime_EasesIn()
        {
            ZoomModelTo(4f);
            _panSettings.Speed = 1f;
            _panSettings.SmoothTime = 0.1f;

            _controller.Tick(new MapInputFrame { Pan = Vector2.right }, 0.1f);

            Assert.That(_model.Pan.x, Is.LessThan(0f).And.GreaterThan(-90f));
        }

        [Test]
        public void DirectionalPan_AfterRelease_ComesToRest()
        {
            ZoomModelTo(4f);
            _panSettings.SmoothTime = 0.1f;
            _controller.Tick(new MapInputFrame { Pan = Vector2.right }, 0.1f);

            for (int frame = 0; frame < 120; frame++)
                _controller.Tick(default, 1f / 60f);
            Vector2 restingPan = _model.Pan;
            _controller.Tick(default, 1f / 60f);

            Assert.That(_model.Pan, Is.EqualTo(restingPan));
        }

        [Test]
        public void ContinuousZoom_Positive_ZoomsInAtDoublingsPerSecond()
        {
            _zoomSettings.Speed = 1f;

            _controller.Tick(new MapInputFrame { Zoom = 1f }, 1f);

            Assert.That(_model.Zoom, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void ContinuousZoom_Negative_ZoomsOut()
        {
            ZoomModelTo(4f);
            _zoomSettings.Speed = 1f;

            _controller.Tick(new MapInputFrame { Zoom = -1f }, 1f);

            Assert.That(_model.Zoom, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void ContinuousZoom_StaysWithinLimits()
        {
            _controller.Tick(new MapInputFrame { Zoom = 1f }, 10f);

            Assert.That(_model.Zoom, Is.EqualTo(4f));
        }

        [Test]
        public void ContinuousZoom_KeepsViewportCenterFixed()
        {
            ZoomModelTo(2f);
            var centeredPoint = new Vector2(0.45f, 0.55f);
            _model.CenterOn(centeredPoint);

            _controller.Tick(new MapInputFrame { Zoom = 1f }, 0.5f);

            AssertApproximately(centeredPoint, _model.ViewportToNormalized(Vector2.zero), 1e-5f);
        }

        [Test]
        public void ZoomSteps_MultiplyZoomPerStep()
        {
            _zoomSettings.StepMultiplier = 1.25f;
            MapInputFrame input = PointerAt(k_ScreenCenter);
            input.ZoomSteps = 2f;

            _controller.Tick(input, 0.016f);

            Assert.That(_model.Zoom, Is.EqualTo(1.5625f).Within(1e-4f));
        }

        [Test]
        public void ZoomSteps_KeepMapPointUnderPointer()
        {
            ZoomModelTo(1.5f);
            MapInputFrame input = PointerAt(k_ScreenCenter + new Vector2(200f, 150f));
            input.ZoomSteps = 3f;
            Vector2 pointUnderPointer = _model.ViewportToNormalized(new Vector2(200f, 150f));

            _controller.Tick(input, 0.016f);

            AssertApproximately(new Vector2(200f, 150f), _model.NormalizedToViewport(pointUnderPointer));
        }

        [Test]
        public void ZoomSteps_PointerOutsideMap_AreIgnored()
        {
            MapInputFrame input = PointerAt(new Vector2(-100f, -100f));
            input.ZoomSteps = 2f;

            _controller.Tick(input, 0.016f);

            Assert.That(_model.Zoom, Is.EqualTo(1f));
        }

        [Test]
        public void ZoomSteps_PointerOutsideMap_ApplyWhenPointerNotRequired()
        {
            _zoomSettings.ScrollOnlyOverMap = false;
            MapInputFrame input = PointerAt(new Vector2(-100f, -100f));
            input.ZoomSteps = 2f;

            _controller.Tick(input, 0.016f);

            Assert.That(_model.Zoom, Is.GreaterThan(1f));
        }

        [Test]
        public void ZoomSmoothing_EasesTowardsAndSettlesOnTarget()
        {
            _zoomSettings.SmoothTime = 0.1f;
            MapInputFrame input = PointerAt(k_ScreenCenter);
            input.ZoomSteps = 1f;

            _controller.Tick(input, 1f / 60f);
            float easedZoom = _model.Zoom;
            for (int frame = 0; frame < 120; frame++)
                _controller.Tick(default, 1f / 60f);

            Assert.That(easedZoom, Is.GreaterThan(1f).And.LessThan(1.25f));
            Assert.That(_model.Zoom, Is.EqualTo(1.25f));
        }

        [Test]
        public void Drag_StartedOnMap_FollowsPointer()
        {
            ZoomModelTo(4f);
            MapInputFrame press = PointerAt(k_ScreenCenter);
            press.DragPressedThisFrame = true;
            press.DragHeld = true;
            MapInputFrame move = PointerAt(k_ScreenCenter + new Vector2(100f, -50f));
            move.DragHeld = true;

            _controller.Tick(press, 0.016f);
            _controller.Tick(move, 0.016f);

            Assert.That(_controller.IsDragging, Is.True);
            AssertApproximately(new Vector2(100f, -50f), _model.Pan);
        }

        [Test]
        public void Drag_StartedOutsideMap_IsIgnored()
        {
            ZoomModelTo(4f);
            MapInputFrame press = PointerAt(new Vector2(-50f, 450f));
            press.DragPressedThisFrame = true;
            press.DragHeld = true;
            MapInputFrame move = PointerAt(new Vector2(300f, 450f));
            move.DragHeld = true;

            _controller.Tick(press, 0.016f);
            _controller.Tick(move, 0.016f);

            Assert.That(_controller.IsDragging, Is.False);
            Assert.That(_model.Pan, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Drag_ButtonAlreadyHeld_DoesNotStartDrag()
        {
            ZoomModelTo(4f);
            MapInputFrame held = PointerAt(k_ScreenCenter);
            held.DragHeld = true;
            MapInputFrame move = PointerAt(k_ScreenCenter + new Vector2(100f, 0f));
            move.DragHeld = true;

            _controller.Tick(held, 0.016f);
            _controller.Tick(move, 0.016f);

            Assert.That(_model.Pan, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Drag_AfterRelease_StopsFollowingPointer()
        {
            ZoomModelTo(4f);
            MapInputFrame press = PointerAt(k_ScreenCenter);
            press.DragPressedThisFrame = true;
            press.DragHeld = true;

            _controller.Tick(press, 0.016f);
            _controller.Tick(PointerAt(k_ScreenCenter), 0.016f);
            _controller.Tick(PointerAt(k_ScreenCenter + new Vector2(100f, 0f)), 0.016f);

            Assert.That(_controller.IsDragging, Is.False);
            Assert.That(_model.Pan, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ResetView_ReturnsToDefaultZoomCenteredOnMap()
        {
            _zoomSettings.DefaultZoom = 2f;
            ZoomModelTo(4f);
            _model.PanBy(new Vector2(500f, 200f));

            _controller.ResetView();

            Assert.That(_model.Zoom, Is.EqualTo(2f));
            Assert.That(_model.Pan, Is.EqualTo(Vector2.zero));
            Assert.That(_controller.TargetZoom, Is.EqualTo(2f));
        }
    }
}
