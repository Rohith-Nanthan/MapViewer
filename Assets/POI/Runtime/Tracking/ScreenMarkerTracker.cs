using System;
using System.Collections.Generic;
using UnityEngine;

namespace POI
{
    public sealed class ScreenMarkerTracker : MarkerTracker
    {
        ScreenMarkerSettings _settings;

        public ScreenMarkerTracker(ScreenMarkerSettings settings)
        {
            Settings = settings;
        }

        public ScreenMarkerSettings Settings
        {
            get => _settings;
            set => _settings = value ?? throw new ArgumentNullException(nameof(value));
        }

        protected override RectTransform Viewport => _settings.Viewport;

        protected override POI_Marker MarkerPrefab => _settings.MarkerPrefab;

        protected override void PlaceMarkers(in POIViewContext context, RectTransform viewport)
        {
            Rect allowedPivotArea = POIMath.GetAllowedPivotArea(viewport.rect, MarkerBounds, _settings.Padding);
            Camera canvasCamera = GetCanvasCamera(context);

            foreach (KeyValuePair<IPointOfInterest, POI_Marker> pair in Markers)
            {
                IPointOfInterest poi = pair.Key;
                POI_Marker marker = pair.Value;
                if (marker == null)
                    continue;

                Vector2 screenPoint = POIMath.ProjectToScreen(context.Camera, poi.Position);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, canvasCamera, out Vector2 localPoint))
                {
                    marker.SetVisible(false);
                    continue;
                }

                marker.SetLocalPosition(POIMath.ClampTowardCenter(allowedPivotArea, localPoint));
                marker.SetIcon(poi.Icon);
                marker.SetDistance(Vector3.Distance(context.PlayerPosition, poi.Position), _settings.DistanceFormat);
                marker.SetVisible(true);
            }
        }
    }
}
