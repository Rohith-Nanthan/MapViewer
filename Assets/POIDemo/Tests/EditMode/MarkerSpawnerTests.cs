using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace POI.Tests
{
    public class MarkerSpawnerTests
    {
        GameObject _root;
        RectTransform _viewport;
        POI_Marker _prefab;
        MarkerSpawner _spawner;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Root", typeof(RectTransform));
            _viewport = (RectTransform)new GameObject("Viewport", typeof(RectTransform)).transform;
            _viewport.SetParent(_root.transform, false);

            // Stands in for a prefab asset: kept outside the viewport, never shown.
            var prefabObject = new GameObject("Marker", typeof(RectTransform), typeof(Image));
            prefabObject.transform.SetParent(_root.transform, false);
            _prefab = prefabObject.AddComponent<POI_Marker>();

            _spawner = new MarkerSpawner(_prefab, _viewport);
        }

        [TearDown]
        public void TearDown()
        {
            _spawner.Dispose();
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void GetOrSpawn_SpawnsHiddenCopyOfPrefabInViewport()
        {
            POI_Marker marker = _spawner.GetOrSpawn(new FakePointOfInterest());

            Assert.That(marker, Is.Not.SameAs(_prefab));
            Assert.That(marker.transform.parent, Is.SameAs(_viewport));
            Assert.That(marker.IsVisible, Is.False);
            Assert.That(_spawner.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetOrSpawn_SamePointOfInterest_ReturnsItsMarker()
        {
            var poi = new FakePointOfInterest();
            POI_Marker first = _spawner.GetOrSpawn(poi);

            Assert.That(_spawner.GetOrSpawn(poi), Is.SameAs(first));
            Assert.That(_spawner.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetOrSpawn_UnderScaledViewport_KeepsPrefabLocalScale()
        {
            _viewport.localScale = new Vector3(2f, 2f, 1f);

            POI_Marker marker = _spawner.GetOrSpawn(new FakePointOfInterest());

            Assert.That(marker.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void Despawn_HidesMarkerAndReusesItForTheNextPoint()
        {
            var poi = new FakePointOfInterest();
            POI_Marker marker = _spawner.GetOrSpawn(poi);
            marker.SetVisible(true);

            Assert.That(_spawner.Despawn(poi), Is.True);

            Assert.That(marker.IsVisible, Is.False);
            Assert.That(_spawner.TryGet(poi, out _), Is.False);
            Assert.That(_spawner.GetOrSpawn(new FakePointOfInterest()), Is.SameAs(marker));
        }

        [Test]
        public void DespawnAll_HidesEveryMarker()
        {
            POI_Marker first = _spawner.GetOrSpawn(new FakePointOfInterest());
            POI_Marker second = _spawner.GetOrSpawn(new FakePointOfInterest());
            first.SetVisible(true);
            second.SetVisible(true);

            _spawner.DespawnAll();

            Assert.That(_spawner.Count, Is.EqualTo(0));
            Assert.That(first.IsVisible || second.IsVisible, Is.False);
        }

        [Test]
        public void Dispose_DestroysSpawnedAndDespawnedMarkers()
        {
            var despawned = new FakePointOfInterest();
            POI_Marker reusable = _spawner.GetOrSpawn(despawned);
            POI_Marker spawned = _spawner.GetOrSpawn(new FakePointOfInterest());
            _spawner.Despawn(despawned);

            _spawner.Dispose();

            Assert.That(reusable == null, Is.True);
            Assert.That(spawned == null, Is.True);
            Assert.That(_spawner.Count, Is.EqualTo(0));
        }
    }
}
