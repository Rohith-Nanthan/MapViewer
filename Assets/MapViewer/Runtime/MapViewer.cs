using System;
using UnityEngine;
using UnityEngine.Events;

namespace Maps
{
    /// <summary>
    /// Pan-and-zoom map for uGUI. Assign any sprite in the Inspector, then call <see cref="Enable"/> and
    /// <see cref="Disable"/> (for example from a menu button) to show and hide it.
    /// </summary>
    /// <remarks>
    /// Enabling activates this GameObject and the map's input actions; disabling deactivates both, so a
    /// hidden map costs nothing. Time is unscaled, so the map still works while a pause menu sets
    /// <see cref="Time.timeScale"/> to 0.
    /// </remarks>
    [AddComponentMenu("Map Viewer/Map Viewer")]
    [DisallowMultipleComponent]
    public sealed class MapViewer : MonoBehaviour, IMapViewer
    {
        // Caps a single step so a frame hitch cannot fling the map while a stick or trigger is held.
        const float k_MaxDeltaTime = 0.1f;

        [Tooltip("The map to display. Any sprite works; its aspect ratio is kept.")]
        [SerializeField] Sprite m_MapSprite;

        [Tooltip("Pan and zoom tuning. Leave empty to use the built-in defaults.")]
        [SerializeField] MapViewerSettings m_Settings;

        [Tooltip("Return to the default zoom, centered on the map, every time the map is enabled.")]
        [SerializeField] bool m_ResetViewOnEnable = true;

        [Tooltip("UI objects that display the map. The prefab already wires them.")]
        [SerializeField] MapView m_View = new MapView();

        [Tooltip("Invoked after the map is enabled.")]
        [SerializeField] UnityEvent m_OnEnabled = new UnityEvent();

        [Tooltip("Invoked after the map is disabled.")]
        [SerializeField] UnityEvent m_OnDisabled = new UnityEvent();

        readonly MapViewportModel _model = new MapViewportModel();
        MapNavigationController _controller;
        IMapInput _input;
        MapViewerSettings _defaultSettings;
        bool _isEnabled;

        public event Action<bool> EnabledChanged;

        public bool IsEnabled => _isEnabled;

        /// <summary>The displayed map. Changing it keeps the current view where the new map allows.</summary>
        public Sprite MapSprite
        {
            get => m_MapSprite;
            set
            {
                m_MapSprite = value;
                m_View.SetSprite(m_MapSprite);
                Refresh();
            }
        }

        /// <summary>Pan and zoom tuning in use. Null falls back to the built-in defaults.</summary>
        public MapViewerSettings Settings
        {
            get => ActiveSettings;
            set
            {
                m_Settings = value;
                Refresh();
            }
        }

        /// <summary>Where the map receives input from. Defaults to the <see cref="IMapInput"/> on this GameObject.</summary>
        public IMapInput InputSource
        {
            get => _input;
            set
            {
                if (_isEnabled)
                    _input?.Disable();

                _input = value ?? NullMapInput.Instance;
                if (_isEnabled)
                    _input.Enable();
            }
        }

        /// <summary>Current pan and zoom, e.g. to place markers with <see cref="MapViewportModel.NormalizedToViewport"/>.</summary>
        public MapViewportModel Model => _model;

        /// <summary>UI presentation of the map. Its viewport can host overlays such as markers.</summary>
        public MapView View => m_View;

        MapViewerSettings ActiveSettings
        {
            get
            {
                if (m_Settings != null)
                    return m_Settings;

                if (_defaultSettings == null)
                {
                    _defaultSettings = ScriptableObject.CreateInstance<MapViewerSettings>();
                    _defaultSettings.name = "Default Map Viewer Settings";
                    _defaultSettings.hideFlags = HideFlags.DontSave;
                }

                return _defaultSettings;
            }
        }

        public void Enable()
        {
            enabled = true;
            gameObject.SetActive(true);
        }

        public void Disable() => gameObject.SetActive(false);

        /// <summary>Disables the map if it is enabled, otherwise enables it.</summary>
        public void Toggle()
        {
            if (enabled && gameObject.activeSelf)
                Disable();
            else
                Enable();
        }

        /// <summary>Returns to the default zoom, centered on the map.</summary>
        public void ResetView()
        {
            if (!TryInitialize())
                return;

            Sync();
            _controller.ResetView();
            Render();
        }

        /// <summary>Centers the view on a normalized map point, e.g. the player's position.</summary>
        public void CenterOn(Vector2 normalizedPoint)
        {
            if (!TryInitialize())
                return;

            Sync();
            _model.CenterOn(normalizedPoint);
            _controller.Stop();
            Render();
        }

        void Awake() => TryInitialize();

        void OnEnable()
        {
            if (!TryInitialize())
                return;

            if (m_ResetViewOnEnable)
                ResetView();

            _input.Enable();
            _isEnabled = true;
            m_OnEnabled.Invoke();
            EnabledChanged?.Invoke(true);
        }

        void OnDisable()
        {
            if (!_isEnabled)
                return;

            _input.Disable();
            _controller.Stop();
            _isEnabled = false;
            m_OnDisabled.Invoke();
            EnabledChanged?.Invoke(false);
        }

        void OnDestroy()
        {
            if (_defaultSettings == null)
                return;

            if (Application.isPlaying)
                Destroy(_defaultSettings);
            else
                DestroyImmediate(_defaultSettings);
        }

        void Update()
        {
            Sync();
            _controller.Tick(_input.ReadFrame(), Mathf.Min(Time.unscaledDeltaTime, k_MaxDeltaTime));
            Render();
        }

        bool TryInitialize()
        {
            if (_controller != null)
                return true;

            if (!m_View.IsValid)
            {
                Debug.LogError($"{nameof(MapViewer)} '{name}' is missing UI references in its View section.", this);
                enabled = false;
                return false;
            }

            if (_input == null)
                _input = TryGetComponent(out IMapInput input) ? input : NullMapInput.Instance;

            m_View.SetSprite(m_MapSprite);
            _controller = new MapNavigationController(_model, m_View, ActiveSettings);
            Sync();
            _controller.Stop();
            return true;
        }

        void Refresh()
        {
            if (_controller == null)
                return;

            Sync();
            Render();
        }

        // Pushes the latest settings and layout into the model.
        void Sync()
        {
            MapViewerSettings activeSettings = ActiveSettings;
            MapZoomSettings zoom = activeSettings.Zoom;
            _controller.Settings = activeSettings;

            m_View.SetFitMode(zoom.FitMode);
            _model.SetFitMode(zoom.FitMode);
            _model.SetZoomLimits(zoom.MinZoom, zoom.MaxZoom);
            _model.SetContentAspect(m_View.ContentAspect);
            _model.SetViewportSize(m_View.Size);
        }

        void Render() => m_View.Render(_model.Pan, _model.Zoom);

#if UNITY_EDITOR
        void OnValidate()
        {
            // Other components cannot be changed safely during OnValidate, so refresh the preview right after.
            UnityEditor.EditorApplication.delayCall -= RefreshPreview;
            UnityEditor.EditorApplication.delayCall += RefreshPreview;
        }

        void RefreshPreview()
        {
            if (this == null || !m_View.IsValid)
                return;

            m_View.SetSprite(m_MapSprite);
            m_View.SetFitMode(m_Settings != null ? m_Settings.Zoom.FitMode : MapFitMode.Fit);
            Refresh();
        }
#endif

        sealed class NullMapInput : IMapInput
        {
            public static readonly NullMapInput Instance = new NullMapInput();

            public bool IsEnabled => false;
            public void Enable() { }
            public void Disable() { }
            public MapInputFrame ReadFrame() => default;
        }
    }
}
