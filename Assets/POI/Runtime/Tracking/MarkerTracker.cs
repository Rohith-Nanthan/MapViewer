using System;
using System.Collections.Generic;
using UnityEngine;

namespace POI
{
    /// <summary>
    /// Base for trackers that show a copy of a marker template per point of interest inside a viewport. It owns
    /// the markers and their layout; subclasses only decide where each marker goes.
    /// </summary>
    public abstract class MarkerTracker : IDisposable
    {
        readonly MarkerSet _markers = new MarkerSet();

        // Points added before the viewport and template are assigned; their markers are made once they are.
        readonly List<IPointOfInterest> _pending = new List<IPointOfInterest>();
        bool _isLayoutDirty = true;

        /// <summary>Rectangle the markers move within, or null while it is not set up.</summary>
        protected abstract RectTransform Viewport { get; }

        /// <summary>Marker copied for each point of interest, or null while it is not set up.</summary>
        protected abstract POI_Marker Template { get; }

        /// <summary>The markers, one per point of interest.</summary>
        protected MarkerSet Markers => _markers;

        /// <summary>Bounds of a marker around its pivot, children included, in the viewport's units.</summary>
        protected Rect MarkerBounds { get; private set; }

        /// <summary>Camera that renders the viewport's canvas, or null for Screen Space - Overlay.</summary>
        protected Camera CanvasCamera { get; private set; }

        /// <summary>Number of points of interest that have a marker.</summary>
        public int MarkerCount => _markers.Count;

        /// <summary>Whether both the viewport and the template are assigned.</summary>
        public bool IsConfigured => Viewport != null && Template != null;

        public bool TryGetMarker(IPointOfInterest poi, out POI_Marker marker) => _markers.TryGet(poi, out marker);

        public void Add(IPointOfInterest poi)
        {
            if (poi == null)
                throw new ArgumentNullException(nameof(poi));

            if (IsConfigured)
                _markers.Add(poi, Template, Viewport);
            else if (!_pending.Contains(poi))
                _pending.Add(poi);
        }

        public void Remove(IPointOfInterest poi)
        {
            _pending.Remove(poi);
            _markers.Remove(poi);
        }

        public void Clear()
        {
            _pending.Clear();
            _markers.Clear();
        }

        public void Hide() => _markers.HideAll();

        public void RefreshLayout() => _isLayoutDirty = true;

        public void Dispose()
        {
            _pending.Clear();
            _markers.Dispose();
        }

        public void Update(in POIViewContext context)
        {
            if (!IsConfigured)
                return;

            if (_pending.Count > 0)
                AddPending();

            if (_markers.Count == 0)
                return;

            RectTransform viewport = Viewport;

            if (_isLayoutDirty)
                ReadLayout(viewport);

            PlaceMarkers(context, viewport);
        }

        /// <summary>
        /// Positions and shows every marker for this frame. <see cref="MarkerBounds"/> says how far each marker
        /// reaches around the position it is given.
        /// </summary>
        protected abstract void PlaceMarkers(in POIViewContext context, RectTransform viewport);

        void AddPending()
        {
            foreach (IPointOfInterest poi in _pending)
                _markers.Add(poi, Template, Viewport);
            _pending.Clear();
        }

        void ReadLayout(RectTransform viewport)
        {
            CanvasCamera = GetCanvasCamera(viewport);

            // Every marker is a copy of the same template, so one marker's size stands for all of them. A hidden
            // marker has no size, so it is shown first; it is placed before anything is drawn.
            foreach (KeyValuePair<IPointOfInterest, POI_Marker> pair in _markers)
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

        // Screen Space - Overlay canvases have no camera, which RectTransformUtility expects as null.
        static Camera GetCanvasCamera(RectTransform viewport)
        {
            Canvas canvas = viewport.GetComponentInParent<Canvas>();
            if (canvas == null)
                return null;

            Canvas rootCanvas = canvas.rootCanvas;
            return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        }
    }
}
