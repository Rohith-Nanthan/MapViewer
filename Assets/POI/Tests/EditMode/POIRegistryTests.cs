using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace POI.Tests
{
    public class POIRegistryTests
    {
        readonly List<IPointOfInterest> _added = new List<IPointOfInterest>();
        readonly List<IPointOfInterest> _removed = new List<IPointOfInterest>();
        POIRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new POIRegistry();
            _registry.Added += _added.Add;
            _registry.Removed += _removed.Add;
        }

        [TearDown]
        public void TearDown()
        {
            _added.Clear();
            _removed.Clear();
        }

        [Test]
        public void Add_ListsItAndRaisesAdded()
        {
            var poi = new FakePointOfInterest();

            Assert.That(_registry.Add(poi), Is.True);

            Assert.That(_registry.Items, Is.EqualTo(new[] { poi }));
            Assert.That(_registry.Contains(poi), Is.True);
            Assert.That(_added, Is.EqualTo(new[] { poi }));
        }

        [Test]
        public void Add_AlreadyListed_IsIgnored()
        {
            var poi = new FakePointOfInterest();
            _registry.Add(poi);

            Assert.That(_registry.Add(poi), Is.False);

            Assert.That(_registry.Count, Is.EqualTo(1));
            Assert.That(_added.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_UnlistsItAndRaisesRemoved()
        {
            var poi = new FakePointOfInterest();
            _registry.Add(poi);

            Assert.That(_registry.Remove(poi), Is.True);

            Assert.That(_registry.Count, Is.EqualTo(0));
            Assert.That(_removed, Is.EqualTo(new[] { poi }));
        }

        [Test]
        public void Remove_NotListed_IsIgnored()
        {
            Assert.That(_registry.Remove(new FakePointOfInterest()), Is.False);
            Assert.That(_registry.Remove(null), Is.False);
            Assert.That(_removed, Is.Empty);
        }

        [Test]
        public void Add_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _registry.Add(null));
        }
    }
}
