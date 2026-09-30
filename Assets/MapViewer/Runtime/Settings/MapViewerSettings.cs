using UnityEngine;

namespace Maps
{
    /// <summary>
    /// Pan and zoom tuning for a <see cref="MapViewer"/>. Share one asset between viewers so they feel
    /// the same, or create variants per menu. Changes made in Play Mode apply immediately.
    /// </summary>
    [CreateAssetMenu(fileName = "MapViewerSettings", menuName = "Map Viewer/Map Viewer Settings")]
    public sealed class MapViewerSettings : ScriptableObject
    {
        [SerializeField] MapPanSettings pan = new MapPanSettings();
        [SerializeField] MapZoomSettings zoom = new MapZoomSettings();

        [Tooltip("Scroll zoom and drag panning only start while the pointer is over the map.")]
        [SerializeField] bool requirePointerOverMap = true;

        public MapPanSettings Pan => pan;
        public MapZoomSettings Zoom => zoom;

        public bool RequirePointerOverMap
        {
            get => requirePointerOverMap;
            set => requirePointerOverMap = value;
        }

        void OnValidate() => zoom.Validate();
    }
}
