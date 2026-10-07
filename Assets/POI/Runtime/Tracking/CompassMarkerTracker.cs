using System;
using System.Collections.Generic;
using UnityEngine;

namespace POI
{
    /// <summary>
    /// Slides a marker for each point of interest along the viewport of its <see cref="CompassMarkerSettings"/>,
    /// by the point's bearing from where the camera faces, and shows the distance from the player in the marker's
    /// label. Only the horizontal position follows the camera; markers keep their template's height.
    /// </summary>
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
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value != _settings)
                    MarkerSet.HideTemplate(value.Template);
                _settings = value;
            }
        }

        protected override RectTransform Viewport => _settings.Viewport;

        protected override POI_Marker Template => _settings.Template;

        protected override void PlaceMarkers(in POIViewContext context, RectTransform viewport)
        {
            Rect area = viewport.rect;
            float padding = _settings.Padding;
            Rect allowedPivotArea = POIMath.GetAllowedPivotArea(area, MarkerBounds, new EdgePadding(padding, padding, 0f, 0f));
            Vector3 heading = POIMath.GetHeading(context.CameraForward, context.CameraUp, Vector3.up);

            foreach (KeyValuePair<IPointOfInterest, POI_Marker> pair in Markers)
            {
                IPointOfInterest poi = pair.Key;
                POI_Marker marker = pair.Value;
                if (marker == null)
                    continue;

                float bearing = POIMath.GetBearing(heading, poi.Position - context.CameraPosition, Vector3.up);
                float x = area.center.x + POIMath.BearingToOffset(bearing, _settings.FieldOfView, area.width);

                // The height comes from the template; it is only clamped so the marker is never cut off.
                float y = marker.RectTransform.localPosition.y;
                marker.SetLocalPosition(new Vector2(
                    Mathf.Clamp(x, allowedPivotArea.xMin, allowedPivotArea.xMax),
                    Mathf.Clamp(y, allowedPivotArea.yMin, allowedPivotArea.yMax)));
                marker.SetSprite(poi.Icon);
                marker.SetDistance(Vector3.Distance(context.PlayerPosition, poi.Position), _settings.DistanceFormat);
                marker.SetVisible(true);
            }
        }
    }
}
