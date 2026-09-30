using NUnit.Framework;
using UnityEngine;

namespace Maps.Tests
{
    public class MapUtilTests
    {
        static readonly Vector2 k_Viewport = new Vector2(1600f, 900f);

        static void AssertApproximately(Vector2 expected, Vector2 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(1e-3f), "x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(1e-3f), "y");
        }

        [TestCase(2f, MapFitMode.Fit, 1600f, 800f)]
        [TestCase(1f, MapFitMode.Fit, 900f, 900f)]
        [TestCase(2f, MapFitMode.Fill, 1800f, 900f)]
        [TestCase(1f, MapFitMode.Fill, 1600f, 1600f)]
        public void FitSize_KeepsAspectAndFitsOrFillsViewport(float aspect, MapFitMode fitMode, float width, float height)
        {
            AssertApproximately(new Vector2(width, height), MapUtil.FitSize(k_Viewport, aspect, fitMode));
        }

        [Test]
        public void FitSize_EmptyViewportOrInvalidAspect_IsZero()
        {
            Assert.That(MapUtil.FitSize(Vector2.zero, 2f, MapFitMode.Fit), Is.EqualTo(Vector2.zero));
            Assert.That(MapUtil.FitSize(k_Viewport, 0f, MapFitMode.Fit), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ClampOffset_StopsContentEdgesAtViewportEdges()
        {
            Vector2 clamped = MapUtil.ClampOffset(new Vector2(5000f, -5000f), new Vector2(3200f, 1600f), k_Viewport);

            AssertApproximately(new Vector2(800f, -350f), clamped);
        }

        [Test]
        public void ClampOffset_ContentSmallerThanViewport_IsCentered()
        {
            Vector2 clamped = MapUtil.ClampOffset(new Vector2(300f, 300f), new Vector2(1600f, 800f), k_Viewport);

            AssertApproximately(Vector2.zero, clamped);
        }

        [Test]
        public void ScaleOffsetAroundPivot_KeepsPointUnderPivot()
        {
            var offset = new Vector2(120f, -40f);
            var pivot = new Vector2(300f, 200f);
            var size = new Vector2(1000f, 500f);
            Vector2 pointUnderPivot = MapUtil.LocalToNormalized(pivot, offset, size);

            Vector2 scaledOffset = MapUtil.ScaleOffsetAroundPivot(offset, pivot, 2.5f);

            AssertApproximately(pivot, MapUtil.NormalizedToLocal(pointUnderPivot, scaledOffset, size * 2.5f));
        }

        [Test]
        public void NormalizedToLocal_MapsCornersAndCenter()
        {
            var offset = new Vector2(10f, 20f);
            var size = new Vector2(400f, 200f);

            AssertApproximately(offset, MapUtil.NormalizedToLocal(MapUtil.NormalizedCenter, offset, size));
            AssertApproximately(new Vector2(-190f, -80f), MapUtil.NormalizedToLocal(Vector2.zero, offset, size));
            AssertApproximately(new Vector2(210f, 120f), MapUtil.NormalizedToLocal(Vector2.one, offset, size));
        }

        [Test]
        public void LocalToNormalized_IsInverseOfNormalizedToLocal()
        {
            var offset = new Vector2(-75f, 33f);
            var size = new Vector2(640f, 360f);
            var point = new Vector2(0.2f, 0.85f);

            AssertApproximately(point, MapUtil.LocalToNormalized(MapUtil.NormalizedToLocal(point, offset, size), offset, size));
        }

        [Test]
        public void ContainsCentered_ChecksEachAxisAgainstHalfSize()
        {
            Assert.That(MapUtil.ContainsCentered(k_Viewport, new Vector2(800f, -450f)), Is.True);
            Assert.That(MapUtil.ContainsCentered(k_Viewport, new Vector2(801f, 0f)), Is.False);
            Assert.That(MapUtil.ContainsCentered(k_Viewport, new Vector2(0f, 451f)), Is.False);
        }

        [Test]
        public void ClampCentered_PullsPointsBackInsideTheRectangle()
        {
            AssertApproximately(new Vector2(800f, -450f), MapUtil.ClampCentered(k_Viewport, new Vector2(2000f, -2000f)));
            AssertApproximately(new Vector2(10f, 20f), MapUtil.ClampCentered(k_Viewport, new Vector2(10f, 20f)));
        }

        [Test]
        public void SmoothingFactor_SnapsWithoutSmoothTimeAndCovers63PercentPerSmoothTime()
        {
            Assert.That(MapUtil.SmoothingFactor(0f, 0.016f), Is.EqualTo(1f));
            Assert.That(MapUtil.SmoothingFactor(0.2f, 0.2f), Is.EqualTo(1f - Mathf.Exp(-1f)).Within(1e-6f));
            Assert.That(MapUtil.SmoothingFactor(0.2f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void LerpLog_MidpointIsGeometricMean()
        {
            Assert.That(MapUtil.LerpLog(1f, 4f, 0.5f), Is.EqualTo(2f).Within(1e-5f));
            Assert.That(MapUtil.LerpLog(2f, 8f, 0f), Is.EqualTo(2f).Within(1e-5f));
        }

        [Test]
        public void GetAspect_UsesSpriteRectOrFallback()
        {
            var texture = new Texture2D(300, 100);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 300f, 100f), MapUtil.NormalizedCenter);
            try
            {
                Assert.That(MapUtil.GetAspect(sprite), Is.EqualTo(3f).Within(1e-5f));
                Assert.That(MapUtil.GetAspect(null, 1.5f), Is.EqualTo(1.5f));
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void TryScreenToCenteredLocal_MeasuresFromRectCenter()
        {
            var rectObject = new GameObject("Rect", typeof(RectTransform));
            try
            {
                var rectTransform = (RectTransform)rectObject.transform;
                rectTransform.sizeDelta = new Vector2(200f, 100f);
                rectTransform.pivot = Vector2.zero;
                rectTransform.position = new Vector3(100f, 100f, 0f);

                bool converted = MapUtil.TryScreenToCenteredLocal(rectTransform, new Vector2(250f, 170f), out Vector2 local);

                Assert.That(converted, Is.True);
                AssertApproximately(new Vector2(50f, 20f), local);
            }
            finally
            {
                Object.DestroyImmediate(rectObject);
            }
        }
    }
}
