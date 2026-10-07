using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace POI
{
    public sealed class MarkerSpawner : IDisposable
    {
        readonly Dictionary<IPointOfInterest, POI_Marker> _spawned = new Dictionary<IPointOfInterest, POI_Marker>();
        readonly Stack<POI_Marker> _despawned = new Stack<POI_Marker>();

        public MarkerSpawner(POI_Marker prefab, RectTransform viewport)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (viewport == null)
                throw new ArgumentNullException(nameof(viewport));

            Prefab = prefab;
            Viewport = viewport;
        }

        public POI_Marker Prefab { get; }

        public RectTransform Viewport { get; }

        public int Count => _spawned.Count;

        public bool TryGet(IPointOfInterest poi, out POI_Marker marker) => _spawned.TryGetValue(poi, out marker);

        public Dictionary<IPointOfInterest, POI_Marker>.Enumerator GetEnumerator() => _spawned.GetEnumerator();

        // Markers start hidden, so they only show once a tracker has placed them.
        public POI_Marker GetOrSpawn(IPointOfInterest poi)
        {
            if (poi == null)
                throw new ArgumentNullException(nameof(poi));

            if (_spawned.TryGetValue(poi, out POI_Marker marker) && marker != null)
                return marker;

            marker = TakeDespawnedMarker();
            if (marker == null)
                marker = SpawnHiddenMarker();

            marker.name = poi is Object source && source != null ? $"{Prefab.name} ({source.name})" : Prefab.name;
            _spawned[poi] = marker;
            return marker;
        }

        public bool Despawn(IPointOfInterest poi)
        {
            if (poi == null || !_spawned.Remove(poi, out POI_Marker marker))
                return false;

            KeepForReuse(marker);
            return true;
        }

        public void DespawnAll()
        {
            foreach (POI_Marker marker in _spawned.Values)
                KeepForReuse(marker);
            _spawned.Clear();
        }

        public void HideAll()
        {
            foreach (POI_Marker marker in _spawned.Values)
            {
                if (marker != null)
                    marker.SetVisible(false);
            }
        }

        public void Dispose()
        {
            foreach (POI_Marker marker in _spawned.Values)
                DestroyMarker(marker);
            _spawned.Clear();

            while (_despawned.Count > 0)
                DestroyMarker(_despawned.Pop());
        }

        POI_Marker SpawnHiddenMarker()
        {
            POI_Marker marker = Object.Instantiate(Prefab, Viewport, false);
            marker.SetVisible(false);
            return marker;
        }

        POI_Marker TakeDespawnedMarker()
        {
            while (_despawned.Count > 0)
            {
                POI_Marker marker = _despawned.Pop();
                if (marker != null)
                    return marker;
            }

            return null;
        }

        void KeepForReuse(POI_Marker marker)
        {
            if (marker == null)
                return;

            marker.SetVisible(false);
            _despawned.Push(marker);
        }

        static void DestroyMarker(POI_Marker marker)
        {
            if (marker == null)
                return;

            // Edit Mode tests spawn markers too, and Destroy is only allowed in Play Mode.
            if (Application.isPlaying)
                Object.Destroy(marker.gameObject);
            else
                Object.DestroyImmediate(marker.gameObject);
        }
    }
}
