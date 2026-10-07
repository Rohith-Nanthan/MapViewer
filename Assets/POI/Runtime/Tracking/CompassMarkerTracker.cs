using System;
using System.Collections.Generic;
using UnityEngine;

namespace POI
{
    public sealed class CompassMarkerTracker : MarkerTracker
    {
        CompassMarkerSettings _settings;

        public CompassMarkerTracker(CompassMarkerSettings settings)
        {
            Settings = settings;
        }

        public CompassMarkerSettings Settings
        {
            get => _settings;
            set => _settings = value ?? throw new ArgumentNullException(nameof(value));
        }

        protected override RectTransform Viewport => _settings.Viewport;

        protected override POI_Marker MarkerPrefab => _settings.MarkerPrefab;

        protected override void PlaceMarkers(in POIViewContext context, RectTransform viewport)
        {
            Rect area = viewport.rect;
            float padding = _settings.Padding;
            Rect allowedPivotArea = POIMath.GetAllowedPivotArea(area, MarkerBounds, new EdgePadding(padding, padding, 0f, 0f));
            Vector3 heading = POIMath.GetHeading(context.CameraForward, context.CameraUp, Vector3.up);

            // The marker and its label are centered vertically as one block.
            float y = allowedPivotArea.center.y;

            foreach (KeyValuePair<IPointOfInterest, POI_Marker> pair in Markers)
            {
                IPointOfInterest poi = pair.Key;
                POI_Marker marker = pair.Value;
                if (marker == null)
                    continue;

                float bearing = POIMath.GetBearing(heading, poi.Position - context.CameraPosition, Vector3.up);
                float x = area.center.x + POIMath.BearingToOffset(bearing, _settings.DegreesAcrossViewport, area.width);

                marker.SetLocalPosition(new Vector2(Mathf.Clamp(x, allowedPivotArea.xMin, allowedPivotArea.xMax), y));
                marker.SetIcon(poi.Icon);
                marker.SetDistance(Vector3.Distance(context.PlayerPosition, poi.Position), _settings.DistanceFormat);
                marker.SetVisible(true);
            }
        }
    }
}
