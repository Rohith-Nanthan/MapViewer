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
        const float RestingPanSpeed = 0.5f;
        const float RestingLogZoomDelta = 1e-4f;

        static readonly Vector2 MapCenter = new Vector2(0.5f, 0.5f);

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
            _model.CenterOn(MapCenter);
            Stop();
        }

        public void Tick(in MapInputFrame input, float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            bool hasPointer = TryGetPointer(input, out Vector2 pointer);
            bool pointerAccepted = !_settings.RequirePointerOverMap || (hasPointer && IsInsideViewport(pointer));

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

            _panVelocity = Vector2.Lerp(_panVelocity, targetVelocity, EaseFactor(pan.SmoothTime, deltaTime));
            if (targetVelocity == Vector2.zero && _panVelocity.sqrMagnitude < RestingPanSpeed * RestingPanSpeed)
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
                _zoomPivot = zoom.ZoomTowardsPointer && hasPointer ? ClampToViewport(pointer) : Vector2.zero;
            }

            _targetZoom = _model.ClampZoom(_targetZoom);
            _model.SetZoom(EaseZoom(_model.Zoom, _targetZoom, zoom.SmoothTime, deltaTime), _zoomPivot);
        }

        bool TryGetPointer(in MapInputFrame input, out Vector2 viewportPosition)
        {
            viewportPosition = default;
            return input.HasPointer && _viewportSpace.TryScreenToViewport(input.PointerPosition, out viewportPosition);
        }

        bool IsInsideViewport(Vector2 point)
        {
            Vector2 halfSize = _model.ViewportSize * 0.5f;
            return Mathf.Abs(point.x) <= halfSize.x && Mathf.Abs(point.y) <= halfSize.y;
        }

        Vector2 ClampToViewport(Vector2 point)
        {
            Vector2 halfSize = _model.ViewportSize * 0.5f;
            return Vector2.Max(-halfSize, Vector2.Min(halfSize, point));
        }

        // Zoom eases in log space so zooming in and out feel symmetrical.
        static float EaseZoom(float current, float target, float smoothTime, float deltaTime)
        {
            float t = EaseFactor(smoothTime, deltaTime);
            float logCurrent = Mathf.Log(current);
            float logTarget = Mathf.Log(target);
            if (t >= 1f || Mathf.Abs(logTarget - logCurrent) < RestingLogZoomDelta)
                return target;

            return Mathf.Exp(Mathf.Lerp(logCurrent, logTarget, t));
        }

        // Frame-rate independent exponential ease: covers ~63% of the remaining distance every smoothTime.
        static float EaseFactor(float smoothTime, float deltaTime) =>
            smoothTime > 0f ? 1f - Mathf.Exp(-deltaTime / smoothTime) : 1f;
    }
}
