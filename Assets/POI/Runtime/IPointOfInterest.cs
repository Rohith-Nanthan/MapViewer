using UnityEngine;

namespace POI
{
    public interface IPointOfInterest
    {
        Sprite Icon { get; }

        Vector3 Position { get; }
    }
}
