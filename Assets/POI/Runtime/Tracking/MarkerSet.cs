using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace POI
{
    /// <summary>
    /// Copies of a marker template inside a viewport, one per point of interest. New markers start hidden, so
    /// they only show once a tracker has placed them. Removed markers are kept for reuse, so points of interest
    /// can be activated and deactivated without instantiating every time.
    /// </summary>
    public sealed class MarkerSet : IDisposable
    {
        readonly Dictionary<IPointOfInterest, POI_Marker> _markers = new Dictionary<IPointOfInterest, POI_Marker>();
        readonly Stack<POI_Marker> _pool = new Stack<POI_Marker>();
        POI_Marker _poolTemplate;
        RectTransform _poolViewport;

        public int Count => _markers.Count;

        public bool TryGet(IPointOfInterest poi, out POI_Marker marker) => _markers.TryGetValue(poi, out marker);

        public Dictionary<IPointOfInterest, POI_Marker>.Enumerator GetEnumerator() => _markers.GetEnumerator();

        /// <summary>Adds a hidden copy of <paramref name="template"/> to <paramref name="viewport"/> for a point of interest.</summary>
        /// <returns>The new marker, or the existing one when the point of interest already has a marker.</returns>
        public POI_Marker Add(IPointOfInterest poi, POI_Marker template, RectTransform viewport)
        {
            if (poi == null)
                throw new ArgumentNullException(nameof(poi));
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            if (viewport == null)
                throw new ArgumentNullException(nameof(viewport));

            if (_markers.TryGetValue(poi, out POI_Marker marker) && marker != null)
                return marker;

            marker = Rent(template, viewport);
            marker.name = poi is Object source && source != null ? $"{template.name} ({source.name})" : template.name;
            _markers[poi] = marker;
            return marker;
        }

        /// <returns>False when the point of interest has no marker.</returns>
        public bool Remove(IPointOfInterest poi)
        {
            if (poi == null || !_markers.Remove(poi, out POI_Marker marker))
                return false;

            Release(marker);
            return true;
        }

        /// <summary>Removes every marker, keeping them for reuse.</summary>
        public void Clear()
        {
            foreach (POI_Marker marker in _markers.Values)
                Release(marker);
            _markers.Clear();
        }

        /// <summary>Hides every marker until it is placed again.</summary>
        public void HideAll()
        {
            foreach (POI_Marker marker in _markers.Values)
            {
                if (marker != null)
                    marker.SetVisible(false);
            }
        }

        /// <summary>Destroys every marker, including the ones kept for reuse.</summary>
        public void Dispose()
        {
            foreach (POI_Marker marker in _markers.Values)
                DestroyMarker(marker);
            _markers.Clear();
            ClearPool();
        }

        /// <summary>Hides a template that is part of a scene, such as one placed in its viewport, so only its copies show.</summary>
        public static void HideTemplate(POI_Marker template)
        {
            if (template != null && template.gameObject.scene.IsValid() && template.gameObject.activeSelf)
                template.gameObject.SetActive(false);
        }

        POI_Marker Rent(POI_Marker template, RectTransform viewport)
        {
            // Markers made from another template, or in another viewport, cannot be reused.
            if (template != _poolTemplate || viewport != _poolViewport)
            {
                ClearPool();
                _poolTemplate = template;
                _poolViewport = viewport;
            }

            while (_pool.Count > 0)
            {
                POI_Marker pooled = _pool.Pop();
                if (pooled != null)
                    return pooled;
            }

            POI_Marker marker = Object.Instantiate(template, viewport, false);
            marker.Template = template;
            marker.SetVisible(false);
            return marker;
        }

        void Release(POI_Marker marker)
        {
            if (marker == null)
                return;

            if (marker.Template != _poolTemplate || marker.transform.parent != _poolViewport)
            {
                DestroyMarker(marker);
                return;
            }

            marker.SetVisible(false);
            _pool.Push(marker);
        }

        void ClearPool()
        {
            while (_pool.Count > 0)
                DestroyMarker(_pool.Pop());
        }

        static void DestroyMarker(POI_Marker marker)
        {
            if (marker != null)
                POIUtil.DestroyObject(marker.gameObject);
        }
    }
}
