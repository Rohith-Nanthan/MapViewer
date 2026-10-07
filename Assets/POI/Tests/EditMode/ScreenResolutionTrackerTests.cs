using NUnit.Framework;
using UnityEngine;

namespace POI.Tests
{
    public class ScreenResolutionTrackerTests
    {
        static readonly ScreenResolution k_Landscape = new ScreenResolution(new Vector2Int(1920, 1080), new Rect(0f, 0f, 1920f, 1080f));

        [Test]
        public void TryUpdate_SameReading_ReportsNoChange()
        {
            var tracker = new ScreenResolutionTracker(k_Landscape);

            Assert.That(tracker.TryUpdate(k_Landscape), Is.False);
            Assert.That(tracker.Current, Is.EqualTo(k_Landscape));
        }

        [Test]
        public void TryUpdate_NewSize_ReportsChangeOnce()
        {
            var tracker = new ScreenResolutionTracker(k_Landscape);
            var portrait = new ScreenResolution(new Vector2Int(1080, 1920), new Rect(0f, 0f, 1080f, 1920f));

            Assert.That(tracker.TryUpdate(portrait), Is.True);
            Assert.That(tracker.TryUpdate(portrait), Is.False);
            Assert.That(tracker.Current, Is.EqualTo(portrait));
        }

        [Test]
        public void TryUpdate_NewSafeAreaAtSameSize_ReportsChange()
        {
            var tracker = new ScreenResolutionTracker(k_Landscape);
            var notched = new ScreenResolution(k_Landscape.Size, new Rect(80f, 0f, 1760f, 1080f));

            Assert.That(tracker.TryUpdate(notched), Is.True);
        }

        [Test]
        public void ScreenResolution_ComparesSizeAndSafeArea()
        {
            var same = new ScreenResolution(new Vector2Int(1920, 1080), new Rect(0f, 0f, 1920f, 1080f));

            Assert.That(same == k_Landscape, Is.True);
            Assert.That(same.GetHashCode(), Is.EqualTo(k_Landscape.GetHashCode()));
            Assert.That(k_Landscape.Aspect, Is.EqualTo(16f / 9f).Within(1e-5f));
            Assert.That(k_Landscape.IsLandscape, Is.True);
        }
    }
}
