using System;
using System.Collections.Generic;
using UnityEngine;

namespace POI
{
    public sealed class POIRegistry
    {
        static POIRegistry s_Default = new POIRegistry();

        readonly List<IPointOfInterest> _items = new List<IPointOfInterest>();

        public static POIRegistry Default => s_Default;

        public event Action<IPointOfInterest> Added;

        public event Action<IPointOfInterest> Removed;

        public IReadOnlyList<IPointOfInterest> Items => _items;

        public int Count => _items.Count;

        public bool Contains(IPointOfInterest poi) => poi != null && _items.Contains(poi);

        // Returns false when the point is already listed, so it can be called again safely.
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

        // Returns false when the point is not listed, so it can be called again safely.
        public bool Remove(IPointOfInterest poi)
        {
            if (poi == null || !_items.Remove(poi))
                return false;

            Removed?.Invoke(poi);
            return true;
        }

        public void RemoveDestroyed()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                IPointOfInterest poi = _items[i];
                if (poi is UnityEngine.Object unityObject && unityObject == null)
                {
                    _items.RemoveAt(i);
                    Removed?.Invoke(poi);
                }
            }
        }

        // Statics survive between Play Mode sessions when Enter Play Mode Options skip the domain reload,
        // so every session starts with an empty registry.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetDefault() => s_Default = new POIRegistry();
    }
}
