using UnityEngine;

namespace POI.Tests
{
    // Never removes itself from the registry, and its position throws once destroyed.
    sealed class LeakyPointOfInterest : MonoBehaviour, IPointOfInterest
    {
        public Sprite Icon { get; set; }

        public Vector3 Position => transform.position;
    }
}
