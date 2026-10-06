using System;
using System.Collections.Generic;
using UnityEngine;

namespace POI
{
    /// <summary>
    /// The points of interest that are currently active. <see cref="POI_World"/> adds itself here when it is
    /// activated and every <see cref="POI_UI"/> shows what is listed, so the world and the UI never reference
    /// each other directly.
    /// </summary>
    public sealed class POIRegistry
    {
        static POIRegistry s_Default = new POIRegistry();

        readonly List<IPointOfInterest> _items = new List<IPointOfInterest>();

        /// <summary>Registry that <see cref="POI_World"/> and <see cref="POI_UI"/> use unless given another one.</summary>
        public static POIRegistry Default => s_Default;

        /// <summary>Raised after a point of interest is added.</summary>
        public event Action<IPointOfInterest> Added;

        /// <summary>Raised after a point of interest is removed.</summary>
        public event Action<IPointOfInterest> Removed;

        /// <summary>Active points of interest, in the order they were added.</summary>
        public IReadOnlyList<IPointOfInterest> Items => _items;

        public int Count => _items.Count;

        public bool Contains(IPointOfInterest poi) => poi != null && _items.Contains(poi);

        /// <returns>False when the point of interest was already listed.</returns>
        public bool Add(IPointOfInterest poi)
        {
            if (poi == null)
                throw new ArgumentNullException(nameof(poi));

            if (_items.Contains(poi))
                return false;

            _items.Add(poi);
            Added?.Invoke(poi);
            return true;
        }

        /// <returns>False when the point of interest was not listed.</returns>
        public bool Remove(IPointOfInterest poi)
        {
            if (poi == null || !_items.Remove(poi))
                return false;

            Removed?.Invoke(poi);
            return true;
        }

        // Statics survive between Play Mode sessions when Enter Play Mode Options skip the domain reload,
        // so every session starts with an empty registry.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetDefault() => s_Default = new POIRegistry();
    }
}
