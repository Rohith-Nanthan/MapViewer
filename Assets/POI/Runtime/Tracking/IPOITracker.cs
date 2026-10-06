using System;

namespace POI
{
    /// <summary>
    /// Shows the active points of interest in one part of the UI, such as on screen or on the compass.
    /// <see cref="POI_UI"/> gives every tracker the same points of interest and camera, so a new kind of
    /// marker, such as a minimap, is added by implementing this rather than by changing the UI.
    /// </summary>
    public interface IPOITracker : IDisposable
    {
        /// <summary>Starts showing a point of interest.</summary>
        void Add(IPointOfInterest poi);

        /// <summary>Stops showing a point of interest.</summary>
        void Remove(IPointOfInterest poi);

        /// <summary>Stops showing every point of interest.</summary>
        void Clear();

        /// <summary>Hides every marker until the next <see cref="Update"/>, e.g. while there is no camera.</summary>
        void Hide();

        /// <summary>Re-reads layout that only changes with the screen or the template, such as the markers' size.</summary>
        void RefreshLayout();

        /// <summary>Places every marker for this frame. Called after the camera has moved.</summary>
        void Update(in POIViewContext context);
    }
}
