using System;
using UnityEngine;

namespace Maps
{
    /// <summary>
    /// Applies <see cref="MapInputFrame"/>s to a <see cref="MapViewportModel"/>: directional input pans at a
    /// speed, drags follow the pointer, continuous input zooms at a rate and zoom steps multiply the zoom,
    /// all shaped by <see cref="MapViewerSettings"/>.
    /// </summary>
    public sealed class MapNavigationController
    {
        // Eased motion below these thresholds snaps to rest, so an idle map stops dirtying the UI.
        const float k_RestingPanSpeed = 0.5f;
        const float k_RestingLogZoomDelta = 1e-4f;

        readonly MapViewportModel _model;
        readonly IMapViewportSpace _viewportSpace;
        MapViewerSettings _settings;

        float _targetZoom;
        Vector2 _zoomPivot;
        Vector2 _panVelocity;
        Vector2 _lastDragPosition;

        public MapNavigationController(MapViewportModel model, IMapViewportSpace viewportSpace, MapViewerSettings settings)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _viewportSpace = viewportSpace ?? throw new ArgumentNullException(nameof(viewportSpace));
            Settings = settings;
            _targetZoom = model.Zoom;
        }

        public MapViewerSettings Settings
        {
            get => _settings;
            set => _settings = value != null ? value : throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Whether a drag that started over the map is in progress.</summary>
        public bool IsDragging { get; private set; }

        /// <summary>Zoom the view is easing towards.</summary>
        public float TargetZoom => _targetZoom;

        /// <summary>Cancels any drag, pan momentum and zoom easing in progress.</summary>
        /// <remarks>Call it after changing the model directly, so easing does not pull the zoom back.</remarks>
        public void Stop()
        {
            IsDragging = false;
            _panVelocity = Vector2.zero;
            _targetZoom = _model.Zoom;
        }

        /// <summary>Jumps to the default zoom, centered on the map.</summary>
        public void ResetView()
        {
            _model.SetZoom(_settings.Zoom.DefaultZoom, Vector2.zero);
            _model.CenterOn(MapUtil.NormalizedCenter);
            Stop();
        }

        public void Tick(in MapInputFrame input, float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            bool hasPointer = TryGetPointer(input, out Vector2 pointer);
            bool pointerAccepted = !_settings.RequirePointerOverMap || (hasPointer && MapUtil.ContainsCentered(_model.ViewportSize, pointer));

            UpdateDrag(input, hasPointer, pointer, pointerAccepted);
            UpdateDirectionalPan(input.Pan, deltaTime);
            UpdateZoom(input, deltaTime, hasPointer, pointer, pointerAccepted);
        }

        void UpdateDrag(in MapInputFrame input, bool hasPointer, Vector2 pointer, bool pointerAccepted)
        {
            if (!input.DragHeld || !hasPointer)
            {
                IsDragging = false;
                return;
            }

            if (!IsDragging)
            {
                // Only a fresh press on the map starts a drag; a button already held elsewhere does not.
                if (input.DragPressedThisFrame && pointerAccepted)
                {
                    IsDragging = true;
                    _lastDragPosition = pointer;
                }

                return;
            }

            Vector2 delta = pointer - _lastDragPosition;
            _lastDragPosition = pointer;
            _model.PanBy(delta * _settings.Pan.DragSensitivity);
        }

        void UpdateDirectionalPan(Vector2 direction, float deltaTime)
        {
            MapPanSettings pan = _settings.Pan;
            Vector2 viewport = _model.ViewportSize;
            float speed = pan.Speed * Mathf.Min(viewport.x, viewport.y);

            // By default the stick moves the view, so the map itself moves the opposite way.
            Vector2 targetVelocity = Vector2.ClampMagnitude(direction, 1f) * (pan.Invert ? speed : -speed);

            _panVelocity = Vector2.Lerp(_panVelocity, targetVelocity, MapUtil.SmoothingFactor(pan.SmoothTime, deltaTime));
            if (targetVelocity == Vector2.zero && _panVelocity.sqrMagnitude < k_RestingPanSpeed * k_RestingPanSpeed)
                _panVelocity = Vector2.zero;

            if (_panVelocity != Vector2.zero)
                _model.PanBy(_panVelocity * deltaTime);
        }

        void UpdateZoom(in MapInputFrame input, float deltaTime, bool hasPointer, Vector2 pointer, bool pointerAccepted)
        {
            MapZoomSettings zoom = _settings.Zoom;

            if (input.Zoom != 0f)
            {
                _targetZoom *= Mathf.Pow(2f, input.Zoom * zoom.Speed * deltaTime);
                _zoomPivot = Vector2.zero;
            }

            if (input.ZoomSteps != 0f && pointerAccepted)
            {
                _targetZoom *= Mathf.Pow(zoom.StepMultiplier, input.ZoomSteps);
                _zoomPivot = zoom.ZoomTowardsPointer && hasPointer ? MapUtil.ClampCentered(_model.ViewportSize, pointer) : Vector2.zero;
            }

            _targetZoom = _model.ClampZoom(_targetZoom);
            _model.SetZoom(EaseZoom(_model.Zoom, _targetZoom, zoom.SmoothTime, deltaTime), _zoomPivot);
        }

        bool TryGetPointer(in MapInputFrame input, out Vector2 viewportPosition)
        {
            viewportPosition = default;
            return input.HasPointer && _viewportSpace.TryScreenToViewport(input.PointerPosition, out viewportPosition);
        }

        // Zoom eases in log space so zooming in and out feel symmetrical, then snaps once it is close enough.
        static float EaseZoom(float current, float target, float smoothTime, float deltaTime)
        {
            float t = MapUtil.SmoothingFactor(smoothTime, deltaTime);
            if (t >= 1f || Mathf.Abs(Mathf.Log(target) - Mathf.Log(current)) < k_RestingLogZoomDelta)
                return target;

            return MapUtil.LerpLog(current, target, t);
        }
    }
}
