using System;
using System.Collections.Generic;
using UnityEngine;

namespace POI
{
    /// <summary>
    /// Shows every active point of interest in two places in the UI: a marker over its position on screen, which
    /// hugs the edge of the screen viewport while the point is out of view, and a marker on the compass, which
    /// slides with the camera's heading and shows the distance from the player.
    /// </summary>
    /// <remarks>
    /// Put it on a Screen Space - Overlay canvas, then assign a viewport and a marker template in the Screen and
    /// Compass sections; leave both empty to skip a section. Points of interest come from
    /// <see cref="POIRegistry"/>, so calling <see cref="POI_World.Activate"/> anywhere is enough. Markers are
    /// placed in LateUpdate, after cameras have moved, so they stay on target however fast the camera moves.
    /// Call <see cref="RefreshLayout"/> from your screen resolution manager when the resolution changes.
    /// </remarks>
    [AddComponentMenu("POI/POI UI")]
    [DefaultExecutionOrder(k_ExecutionOrder)]
    [DisallowMultipleComponent]
    public sealed class POI_UI : MonoBehaviour
    {
        // After camera controllers, Cinemachine included, have moved the camera for this frame.
        const int k_ExecutionOrder = 10000;

        [Tooltip("Camera the markers are relative to. Uses the main camera when empty.")]
        [SerializeField] Camera m_Camera;

        [Tooltip("Distances on the markers are measured from here. Uses the camera when empty.")]
        [SerializeField] Transform m_Player;

        [SerializeField] ScreenMarkerSettings m_Screen = new ScreenMarkerSettings();

        [SerializeField] CompassMarkerSettings m_Compass = new CompassMarkerSettings();

        readonly List<IPOITracker> _trackers = new List<IPOITracker>();
        ScreenMarkerTracker _screenTracker;
        CompassMarkerTracker _compassTracker;
        POIRegistry _registry;
        POIRegistry _subscribedRegistry;
        bool _isLayoutDirty = true;

        /// <summary>Camera the markers are relative to: the assigned one, or else the main camera.</summary>
        public Camera Camera
        {
            get => m_Camera != null ? m_Camera : Camera.main;
            set => m_Camera = value;
        }

        /// <summary>Where distances are measured from. When null, the camera's position is used.</summary>
        public Transform Player
        {
            get => m_Player;
            set => m_Player = value;
        }

        /// <summary>Where the points of interest come from. Defaults to <see cref="POIRegistry.Default"/>.</summary>
        public POIRegistry Registry
        {
            get => _registry ?? POIRegistry.Default;
            set
            {
                bool wasSubscribed = _subscribedRegistry != null;
                UnsubscribeFromRegistry();
                _registry = value;
                if (wasSubscribed)
                    SubscribeToRegistry();
            }
        }

        /// <summary>Settings of the screen markers. Changes apply on the next frame.</summary>
        public ScreenMarkerSettings ScreenSettings
        {
            get => m_Screen;
            set => m_Screen = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Settings of the compass markers. Changes apply on the next frame.</summary>
        public CompassMarkerSettings CompassSettings
        {
            get => m_Compass;
            set => m_Compass = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Places the screen markers, e.g. to find the marker shown for a point of interest.</summary>
        public ScreenMarkerTracker ScreenTracker
        {
            get
            {
                InitializeTrackers();
                return _screenTracker;
            }
        }

        /// <summary>Places the compass markers, e.g. to find the marker shown for a point of interest.</summary>
        public CompassMarkerTracker CompassTracker
        {
            get
            {
                InitializeTrackers();
                return _compassTracker;
            }
        }

        /// <summary>
        /// Adds another kind of marker, such as a minimap, fed the same points of interest and camera. It is
        /// disposed with this component unless it is removed first.
        /// </summary>
        public void AddTracker(IPOITracker tracker)
        {
            if (tracker == null)
                throw new ArgumentNullException(nameof(tracker));

            InitializeTrackers();
            if (_trackers.Contains(tracker))
                return;

            _trackers.Add(tracker);
            tracker.RefreshLayout();
            if (_subscribedRegistry != null)
            {
                foreach (IPointOfInterest poi in _subscribedRegistry.Items)
                    tracker.Add(poi);
            }
        }

        /// <summary>Stops feeding a tracker and removes its markers. The caller then owns it and disposes it.</summary>
        public bool RemoveTracker(IPOITracker tracker)
        {
            if (tracker == null || !_trackers.Remove(tracker))
                return false;

            tracker.Clear();
            return true;
        }

        /// <summary>
        /// Re-reads the layout in this frame's LateUpdate. Call it when the screen resolution changes, or after
        /// resizing a marker at runtime.
        /// </summary>
        public void RefreshLayout() => _isLayoutDirty = true;

        void Awake() => InitializeTrackers();

        void OnEnable()
        {
            SubscribeToRegistry();
            RefreshLayout();
        }

        void OnDisable() => UnsubscribeFromRegistry();

        void OnDestroy()
        {
            foreach (IPOITracker tracker in _trackers)
                tracker.Dispose();
            _trackers.Clear();
        }

        void LateUpdate()
        {
            // The Inspector can replace the settings objects, so hand the trackers the current ones.
            _screenTracker.Settings = m_Screen;
            _compassTracker.Settings = m_Compass;

            if (_isLayoutDirty)
            {
                _isLayoutDirty = false;

                // Let the canvas catch up with the screen first, so the layout read is this frame's.
                Canvas.ForceUpdateCanvases();
                foreach (IPOITracker tracker in _trackers)
                    tracker.RefreshLayout();
            }

            Camera viewCamera = Camera;
            if (viewCamera == null)
            {
                foreach (IPOITracker tracker in _trackers)
                    tracker.Hide();
                return;
            }

            var context = new POIViewContext(viewCamera, m_Player != null ? m_Player.position : viewCamera.transform.position);
            foreach (IPOITracker tracker in _trackers)
                tracker.Update(context);
        }

        // Runs on first use, which can come before Awake, e.g. when AddTracker is called on an inactive object.
        void InitializeTrackers()
        {
            if (_screenTracker != null)
                return;

            _screenTracker = new ScreenMarkerTracker(m_Screen);
            _compassTracker = new CompassMarkerTracker(m_Compass);
            _trackers.Insert(0, _screenTracker);
            _trackers.Insert(1, _compassTracker);
        }

        void SubscribeToRegistry()
        {
            if (_subscribedRegistry != null)
                return;

            _subscribedRegistry = Registry;
            _subscribedRegistry.Added += OnAdded;
            _subscribedRegistry.Removed += OnRemoved;
            foreach (IPointOfInterest poi in _subscribedRegistry.Items)
                OnAdded(poi);
        }

        void UnsubscribeFromRegistry()
        {
            if (_subscribedRegistry == null)
                return;

            _subscribedRegistry.Added -= OnAdded;
            _subscribedRegistry.Removed -= OnRemoved;
            _subscribedRegistry = null;
            foreach (IPOITracker tracker in _trackers)
                tracker.Clear();
        }

        void OnAdded(IPointOfInterest poi)
        {
            InitializeTrackers();
            foreach (IPOITracker tracker in _trackers)
                tracker.Add(poi);
        }

        void OnRemoved(IPointOfInterest poi)
        {
            foreach (IPOITracker tracker in _trackers)
                tracker.Remove(poi);
        }
    }
}
