using UnityEngine;

namespace POI.Tests
{
    sealed class DestroyablePointOfInterest : ScriptableObject, IPointOfInterest
    {
        public Sprite Icon => null;

        public Vector3 Position => Vector3.zero;
    }
}
