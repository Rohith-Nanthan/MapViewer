using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace POI.Tests
{
    public class MarkerSetTests
    {
        GameObject _root;
        RectTransform _viewport;
        POI_Marker _template;
        MarkerSet _markers;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Viewport", typeof(RectTransform));
            _viewport = (RectTransform)_root.transform;
            _template = CreateTemplate("Template");
            _markers = new MarkerSet();
        }

        [TearDown]
        public void TearDown()
        {
            _markers.Dispose();
            Object.DestroyImmediate(_root);
        }

        POI_Marker CreateTemplate(string name)
        {
            var template = new GameObject(name, typeof(RectTransform), typeof(Image)).AddComponent<POI_Marker>();
            template.transform.SetParent(_viewport, false);
            return template;
        }

        [Test]
        public void Add_CreatesHiddenCopyOfTemplateInViewport()
        {
            POI_Marker marker = _markers.Add(new FakePointOfInterest(), _template, _viewport);

            Assert.That(marker, Is.Not.SameAs(_template));
            Assert.That(marker.transform.parent, Is.SameAs(_viewport));
            Assert.That(marker.IsVisible, Is.False);
            Assert.That(_markers.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_SamePointOfInterest_ReturnsItsMarker()
        {
            var poi = new FakePointOfInterest();
            POI_Marker first = _markers.Add(poi, _template, _viewport);

            Assert.That(_markers.Add(poi, _template, _viewport), Is.SameAs(first));
            Assert.That(_markers.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_HidesMarkerAndReusesItForTheNextOne()
        {
            var poi = new FakePointOfInterest();
            POI_Marker marker = _markers.Add(poi, _template, _viewport);
            marker.SetVisible(true);

            Assert.That(_markers.Remove(poi), Is.True);

            Assert.That(marker.IsVisible, Is.False);
            Assert.That(_markers.TryGet(poi, out _), Is.False);
            Assert.That(_markers.Add(new FakePointOfInterest(), _template, _viewport), Is.SameAs(marker));
        }

        [Test]
        public void Remove_MarkerOfReplacedTemplate_DestroysIt()
        {
            var first = new FakePointOfInterest();
            POI_Marker oldMarker = _markers.Add(first, _template, _viewport);
            _markers.Add(new FakePointOfInterest(), CreateTemplate("Other Template"), _viewport);

            _markers.Remove(first);

            Assert.That(oldMarker == null, Is.True);
        }

        [Test]
        public void Dispose_DestroysActiveAndReusableMarkers()
        {
            var removed = new FakePointOfInterest();
            POI_Marker pooled = _markers.Add(removed, _template, _viewport);
            POI_Marker active = _markers.Add(new FakePointOfInterest(), _template, _viewport);
            _markers.Remove(removed);

            _markers.Dispose();

            Assert.That(pooled == null, Is.True);
            Assert.That(active == null, Is.True);
            Assert.That(_markers.Count, Is.EqualTo(0));
        }

        [Test]
        public void HideTemplate_HidesTemplateInScene()
        {
            MarkerSet.HideTemplate(_template);

            Assert.That(_template.gameObject.activeSelf, Is.False);
        }
    }
}
