using UnityEngine;

namespace POI
{
    /// <summary>
    /// Something the UI can point at: an icon and a world position. <see cref="POI_World"/> is the scene
    /// component; other sources, such as a quest system, can implement this and add themselves to a
    /// <see cref="POIRegistry"/> without a GameObject.
    /// </summary>
    public interface IPointOfInterest
    {
        /// <summary>Icon shown on screen and on the compass.</summary>
        Sprite Icon { get; }

        /// <summary>World position the markers point at.</summary>
        Vector3 Position { get; }
    }
}
