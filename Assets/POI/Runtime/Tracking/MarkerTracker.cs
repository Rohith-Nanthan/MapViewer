using System;
using System.Collections.Generic;
using UnityEngine;

namespace POI
{
    public abstract class MarkerTracker : IDisposable
    {
        // Points waiting for a viewport and marker prefab, or for a respawn after either changed.
        readonly List<IPointOfInterest> _pending = new List<IPointOfInterest>();
        MarkerSpawner _spawner;
        Canvas _rootCanvas;
        bool _isLayoutDirty = true;

        protected abstract RectTransform Viewport { get; }

        protected abstract POI_Marker MarkerPrefab { get; }

        protected MarkerSpawner Markers => _spawner;

        protected Rect MarkerBounds { get; private set; }

        public int MarkerCount => _spawner != null ? _spawner.Count : 0;

        public bool IsConfigured => Viewport != null && MarkerPrefab != null;

        public bool TryGetMarker(IPointOfInterest poi, out POI_Marker marker)
        {
            marker = null;
            return _spawner != null && _spawner.TryGet(poi, out marker);
        }

        public void Add(IPointOfInterest poi)
        {
            if (poi == null)
                throw new ArgumentNullException(nameof(poi));

            if (EnsureSpawnerMatchesSettings())
                _spawner.GetOrSpawn(poi);
            else if (!_pending.Contains(poi))
                _pending.Add(poi);
        }

        public void Remove(IPointOfInterest poi)
        {
            _pending.Remove(poi);
            _spawner?.Despawn(poi);
        }

        public void Clear()
        {
            _pending.Clear();
            _spawner?.DespawnAll();
        }

        public void Hide() => _spawner?.HideAll();

        public void RefreshLayout() => _isLayoutDirty = true;

        public void Dispose()
        {
            _pending.Clear();
            DisposeSpawner();
        }

        public void Update(in POIViewContext context)
        {
            if (!EnsureSpawnerMatchesSettings())
                return;

            if (_pending.Count > 0)
                SpawnPending();

            if (_spawner.Count == 0)
                return;

            if (_isLayoutDirty)
                ReadLayout();

            PlaceMarkers(context, _spawner.Viewport);
        }

        protected abstract void PlaceMarkers(in POIViewContext context, RectTransform viewport);

        // A changed viewport or marker prefab respawns every marker, so none is left under the old viewport.
        bool EnsureSpawnerMatchesSettings()
        {
            RectTransform viewport = Viewport;
            POI_Marker markerPrefab = MarkerPrefab;
            bool isConfigured = viewport != null && markerPrefab != null;
            if (_spawner != null && isConfigured && _spawner.Viewport == viewport && _spawner.Prefab == markerPrefab)
                return true;

            if (_spawner != null)
            {
                foreach (KeyValuePair<IPointOfInterest, POI_Marker> pair in _spawner)
                {
                    if (!_pending.Contains(pair.Key))
                        _pending.Add(pair.Key);
                }

                DisposeSpawner();
            }

            if (!isConfigured)
                return false;

            _spawner = new MarkerSpawner(markerPrefab, viewport);
            _isLayoutDirty = true;
            return true;
        }

        void SpawnPending()
        {
            foreach (IPointOfInterest poi in _pending)
                _spawner.GetOrSpawn(poi);
            _pending.Clear();
        }

        // Read every frame, so a canvas camera assigned or replaced later is picked up. RectTransformUtility expects
        // null for Screen Space - Overlay, and for Screen Space - Camera without a camera, which renders as Overlay.
        protected Camera GetCanvasCamera(in POIViewContext context)
        {
            if (_rootCanvas == null || _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            if (_rootCanvas.worldCamera != null)
                return _rootCanvas.worldCamera;

            return _rootCanvas.renderMode == RenderMode.WorldSpace ? context.Camera : null;
        }

        void ReadLayout()
        {
            _rootCanvas = FindRootCanvas(_spawner.Viewport);

            // Every marker is a copy of one prefab, so one marker's size stands for all of them. A hidden marker has
            // no size, so it is shown first; it is placed before anything is drawn.
            foreach (KeyValuePair<IPointOfInterest, POI_Marker> pair in _spawner)
            {
                POI_Marker marker = pair.Value;
                if (marker == null)
                    continue;

                marker.SetVisible(true);
                MarkerBounds = POIMath.GetBoundsAroundPivot(marker.RectTransform);
                _isLayoutDirty = false;
                return;
            }
        }

        void DisposeSpawner()
        {
            _spawner?.Dispose();
            _spawner = null;
        }

        static Canvas FindRootCanvas(RectTransform viewport)
        {
            Canvas[] canvases = viewport.GetComponentsInParent<Canvas>(true);
            return canvases.Length > 0 ? canvases[canvases.Length - 1] : null;
        }
    }
}
