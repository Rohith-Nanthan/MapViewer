using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace POI
{
    public sealed class MarkerSpawner : IDisposable
    {
        readonly Dictionary<IPointOfInterest, POI_Marker> _spawned = new Dictionary<IPointOfInterest, POI_Marker>();
        readonly ObjectPool<POI_Marker> _pool;

        public MarkerSpawner(POI_Marker prefab, RectTransform viewport)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (viewport == null)
                throw new ArgumentNullException(nameof(viewport));

            Prefab = prefab;
            Viewport = viewport;
            _pool = new ObjectPool<POI_Marker>(SpawnHiddenMarker, actionOnRelease: HideMarker, actionOnDestroy: DestroyMarker);
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

            marker = GetLiveMarkerFromPool();
            marker.name = poi is Object source && source != null ? $"{Prefab.name} ({source.name})" : Prefab.name;
            _spawned[poi] = marker;
            return marker;
        }

        public bool Despawn(IPointOfInterest poi)
        {
            if (poi == null || !_spawned.Remove(poi, out POI_Marker marker))
                return false;

            ReleaseToPool(marker);
            return true;
        }

        public void DespawnAll()
        {
            foreach (POI_Marker marker in _spawned.Values)
                ReleaseToPool(marker);
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
            _pool.Clear();
        }

        POI_Marker SpawnHiddenMarker()
        {
            POI_Marker marker = Object.Instantiate(Prefab, Viewport, false);
            marker.SetVisible(false);
            return marker;
        }

        // The pool still hands out a pooled marker that something else destroyed, so those are skipped.
        POI_Marker GetLiveMarkerFromPool()
        {
            POI_Marker marker = _pool.Get();
            while (marker == null)
                marker = _pool.Get();
            return marker;
        }

        void ReleaseToPool(POI_Marker marker)
        {
            if (marker != null)
                _pool.Release(marker);
        }

        static void HideMarker(POI_Marker marker) => marker.SetVisible(false);

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
