using System;
using System.Collections.Generic;
using UnityEngine;

namespace POI
{
    /// <summary>
    /// Places a marker over each point of interest's position on screen, inside the viewport of its
    /// <see cref="ScreenMarkerSettings"/>. A point out of view, behind the camera included, hugs the viewport's
    /// edge in its direction. Markers, children included, always stay fully inside, the padding away from the edges.
    /// </summary>
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
            Rect allowedPivotArea = POIMath.GetAllowedPivotArea(viewport.rect, MarkerBounds, _settings.Padding);

            foreach (KeyValuePair<IPointOfInterest, POI_Marker> pair in Markers)
            {
                IPointOfInterest poi = pair.Key;
                POI_Marker marker = pair.Value;
                if (marker == null)
                    continue;

                Vector2 screenTarget = POIMath.ProjectToScreen(context.Camera, poi.Position);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenTarget, CanvasCamera, out Vector2 localTarget))
                {
                    marker.SetVisible(false);
                    continue;
                }

                marker.SetLocalPosition(POIMath.ClampTowardCenter(allowedPivotArea, localTarget));
                marker.SetIcon(poi.Icon);
                marker.SetDistance(Vector3.Distance(context.PlayerPosition, poi.Position), _settings.DistanceFormat);
                marker.SetVisible(true);
            }
        }
    }
}
