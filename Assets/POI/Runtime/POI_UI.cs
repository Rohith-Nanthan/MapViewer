using System;
using UnityEngine;

namespace POI
{
    [AddComponentMenu("POI/POI UI")]
    [DefaultExecutionOrder(k_ExecutionOrder)]
    [DisallowMultipleComponent]
    public sealed class POI_UI : MonoBehaviour
    {
        // Runs after camera controllers, Cinemachine included, so markers use this frame's camera pose.
        const int k_ExecutionOrder = 10000;

        [Tooltip("Camera the markers are relative to. Uses the main camera when empty.")]
        [SerializeField] Camera m_Camera;

        [Tooltip("Distances on the markers are measured from here. Uses the camera when empty.")]
        [SerializeField] Transform m_Player;

        [SerializeField] ScreenMarkerSettings m_Screen = new ScreenMarkerSettings();

        [SerializeField] CompassMarkerSettings m_Compass = new CompassMarkerSettings();

        ScreenMarkerTracker _screenTracker;
        CompassMarkerTracker _compassTracker;
        MarkerTracker[] _trackers;
        POIRegistry _subscribedRegistry;
        bool _isLayoutDirty = true;

        public Camera Camera
        {
            get => m_Camera != null ? m_Camera : Camera.main;
            set => m_Camera = value;
        }

        public Transform Player
        {
            get => m_Player;
            set => m_Player = value;
        }

        public ScreenMarkerSettings ScreenSettings
        {
            get => m_Screen;
            set => m_Screen = value ?? throw new ArgumentNullException(nameof(value));
        }

        public CompassMarkerSettings CompassSettings
        {
            get => m_Compass;
            set => m_Compass = value ?? throw new ArgumentNullException(nameof(value));
        }

        public ScreenMarkerTracker ScreenTracker
        {
            get
            {
                EnsureTrackersCreated();
                return _screenTracker;
            }
        }

        public CompassMarkerTracker CompassTracker
        {
            get
            {
                EnsureTrackersCreated();
                return _compassTracker;
            }
        }

        /// <summary>Call when the screen resolution changes, or after resizing a marker at runtime.</summary>
        public void RefreshLayout() => _isLayoutDirty = true;

        void Awake() => EnsureTrackersCreated();

        void OnEnable()
        {
            SubscribeToRegistry();
            RefreshLayout();
        }

        void OnDisable() => UnsubscribeFromRegistry();

        void OnDestroy()
        {
            foreach (MarkerTracker tracker in _trackers)
                tracker.Dispose();
        }

        void LateUpdate()
        {
            // A point destroyed without removing itself would otherwise keep its markers.
            _subscribedRegistry.RemoveDestroyed();

            // The Inspector can replace the settings objects, so the trackers are handed the current ones.
            _screenTracker.Settings = m_Screen;
            _compassTracker.Settings = m_Compass;

            if (_isLayoutDirty)
            {
                _isLayoutDirty = false;

                // The canvas catches up with the screen first, so the layout read is this frame's.
                Canvas.ForceUpdateCanvases();
                foreach (MarkerTracker tracker in _trackers)
                    tracker.RefreshLayout();
            }

            Camera viewCamera = Camera;
            if (viewCamera == null)
            {
                foreach (MarkerTracker tracker in _trackers)
                    tracker.Hide();
                return;
            }

            Vector3 playerPosition = m_Player != null ? m_Player.position : viewCamera.transform.position;
            var context = new POIViewContext(viewCamera, playerPosition);
            foreach (MarkerTracker tracker in _trackers)
                tracker.Update(context);
        }

        // The tracker getters can run before Awake, e.g. from the Inspector while this object is inactive.
        void EnsureTrackersCreated()
        {
            if (_trackers != null)
                return;

            _screenTracker = new ScreenMarkerTracker(m_Screen);
            _compassTracker = new CompassMarkerTracker(m_Compass);
            _trackers = new MarkerTracker[] { _screenTracker, _compassTracker };
        }

        void SubscribeToRegistry()
        {
            if (_subscribedRegistry != null)
                return;

            EnsureTrackersCreated();
            _subscribedRegistry = POIRegistry.Default;
            _subscribedRegistry.Added += OnPointAdded;
            _subscribedRegistry.Removed += OnPointRemoved;
            foreach (IPointOfInterest poi in _subscribedRegistry.Items)
                OnPointAdded(poi);
        }

        void UnsubscribeFromRegistry()
        {
            if (_subscribedRegistry == null)
                return;

            _subscribedRegistry.Added -= OnPointAdded;
            _subscribedRegistry.Removed -= OnPointRemoved;
            _subscribedRegistry = null;
            foreach (MarkerTracker tracker in _trackers)
                tracker.Clear();
        }

        void OnPointAdded(IPointOfInterest poi)
        {
            foreach (MarkerTracker tracker in _trackers)
                tracker.Add(poi);
        }

        void OnPointRemoved(IPointOfInterest poi)
        {
            foreach (MarkerTracker tracker in _trackers)
                tracker.Remove(poi);
        }
    }
}
