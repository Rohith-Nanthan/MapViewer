using UnityEngine;

namespace Maps
{
    /// <summary>Converts screen positions into the viewport space used by <see cref="MapViewportModel"/>.</summary>
    public interface IMapViewportSpace
    {
        /// <summary>Converts a screen position to viewport space (local units, origin at the viewport center).</summary>
        /// <returns>False when the position cannot be projected onto the viewport.</returns>
        bool TryScreenToViewport(Vector2 screenPosition, out Vector2 viewportPosition);
    }
}
