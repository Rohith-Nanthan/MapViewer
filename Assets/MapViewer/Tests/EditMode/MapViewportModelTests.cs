using NUnit.Framework;
using UnityEngine;

namespace Maps.Tests
{
    public class MapViewportModelTests
    {
        static readonly Vector2 Viewport = new Vector2(1600f, 900f);

        static MapViewportModel CreateModel(float contentAspect = 2f, MapFitMode fitMode = MapFitMode.Fit)
        {
            var model = new MapViewportModel();
            model.SetZoomLimits(1f, 4f);
            model.SetFitMode(fitMode);
            model.SetContentAspect(contentAspect);
            model.SetViewportSize(Viewport);
            return model;
        }

        static void AssertApproximately(Vector2 expected, Vector2 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(1e-3f), "x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(1e-3f), "y");
        }

        [Test]
        public void Fit_MapWiderThanViewport_MatchesViewportWidth()
        {
            MapViewportModel model = CreateModel(contentAspect: 2f);

            AssertApproximately(new Vector2(1600f, 800f), model.BaseContentSize);
        }

        [Test]
        public void Fit_MapTallerThanViewport_MatchesViewportHeight()
        {
            MapViewportModel model = CreateModel(contentAspect: 1f);

            AssertApproximately(new Vector2(900f, 900f), model.BaseContentSize);
        }

        [Test]
        public void Fill_MapWiderThanViewport_MatchesViewportHeight()
        {
            MapViewportModel model = CreateModel(contentAspect: 2f, fitMode: MapFitMode.Fill);

            AssertApproximately(new Vector2(1800f, 900f), model.BaseContentSize);
        }

        [Test]
        public void SetContentAspect_InvalidValue_FallsBackToSquare()
        {
            MapViewportModel model = CreateModel();

            model.SetContentAspect(0f);

            Assert.That(model.ContentAspect, Is.EqualTo(1f));
        }

        [TestCase(0.5f, 1f)]
        [TestCase(2.5f, 2.5f)]
        [TestCase(10f, 4f)]
        public void SetZoom_IsClampedToLimits(float requested, float expected)
        {
            MapViewportModel model = CreateModel();

            model.SetZoom(requested, Vector2.zero);

            Assert.That(model.Zoom, Is.EqualTo(expected).Within(1e-5f));
        }

        [Test]
        public void SetZoom_KeepsMapPointUnderPivot()
        {
            MapViewportModel model = CreateModel();
            var pivot = new Vector2(400f, 100f);
            Vector2 pointUnderPivot = model.ViewportToNormalized(pivot);

            model.SetZoom(2f, pivot);

            AssertApproximately(pivot, model.NormalizedToViewport(pointUnderPivot));
        }

        [Test]
        public void SetZoomLimits_ClampsCurrentZoom()
        {
            MapViewportModel model = CreateModel();
            model.SetZoom(4f, Vector2.zero);

            model.SetZoomLimits(1f, 2f);

            Assert.That(model.Zoom, Is.EqualTo(2f));
        }

        [Test]
        public void PanBy_StopsAtMapEdges()
        {
            MapViewportModel model = CreateModel();
            model.SetZoom(2f, Vector2.zero);

            model.PanBy(new Vector2(10000f, -10000f));

            // Map is 3200 x 1600 at 2x, so it can move (3200 - 1600) / 2 and (1600 - 900) / 2.
            AssertApproximately(new Vector2(800f, -350f), model.Pan);
        }

        [Test]
        public void PanBy_MapSmallerThanViewport_StaysCentered()
        {
            MapViewportModel model = CreateModel();

            model.PanBy(new Vector2(250f, 250f));

            AssertApproximately(Vector2.zero, model.Pan);
        }

        [Test]
        public void CenterOn_PutsMapPointAtViewportCenter()
        {
            MapViewportModel model = CreateModel();
            model.SetZoom(4f, Vector2.zero);
            var point = new Vector2(0.6f, 0.4f);

            model.CenterOn(point);

            AssertApproximately(Vector2.zero, model.NormalizedToViewport(point));
        }

        [Test]
        public void CenterOn_MapCorner_IsClampedToMapEdges()
        {
            MapViewportModel model = CreateModel();
            model.SetZoom(2f, Vector2.zero);

            model.CenterOn(Vector2.zero);

            AssertApproximately(new Vector2(800f, 350f), model.Pan);
        }

        [Test]
        public void ViewportToNormalized_IsInverseOfNormalizedToViewport()
        {
            MapViewportModel model = CreateModel();
            model.SetZoom(3f, new Vector2(-200f, 150f));
            var point = new Vector2(0.25f, 0.8f);

            Vector2 roundTrip = model.ViewportToNormalized(model.NormalizedToViewport(point));

            AssertApproximately(point, roundTrip);
        }

        [Test]
        public void SetViewportSize_KeepsSameMapPointCentered()
        {
            MapViewportModel model = CreateModel();
            model.SetZoom(3f, Vector2.zero);
            var centeredPoint = new Vector2(0.3f, 0.7f);
            model.CenterOn(centeredPoint);

            model.SetViewportSize(Viewport * 0.5f);

            AssertApproximately(centeredPoint, model.ViewportToNormalized(Vector2.zero));
        }

        [Test]
        public void SetFitMode_ToFill_LetsCroppedAxisPan()
        {
            MapViewportModel model = CreateModel(contentAspect: 2f);

            model.SetFitMode(MapFitMode.Fill);
            model.PanBy(new Vector2(1000f, 0f));

            // Filling a 1600 x 900 viewport makes the map 1800 x 900, leaving 100 units of travel.
            AssertApproximately(new Vector2(100f, 0f), model.Pan);
        }
    }
}
