using System;

namespace Maps
{
    /// <summary>What a menu needs to show and hide a map. Depend on this rather than the component.</summary>
    public interface IMapViewer
    {
        /// <summary>Whether the map is visible and accepting input.</summary>
        bool IsEnabled { get; }

        /// <summary>Raised with <c>true</c> after the map is enabled and <c>false</c> after it is disabled.</summary>
        event Action<bool> EnabledChanged;

        /// <summary>Makes the map visible and enables all of its input actions.</summary>
        void Enable();

        /// <summary>Hides the map and disables all of its input actions.</summary>
        void Disable();
    }
}
