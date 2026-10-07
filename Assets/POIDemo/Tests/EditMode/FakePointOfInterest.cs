using UnityEngine;

namespace POI.Tests
{
    sealed class FakePointOfInterest : IPointOfInterest
    {
        public Sprite Icon { get; set; }

        public Vector3 Position { get; set; }
    }
}
