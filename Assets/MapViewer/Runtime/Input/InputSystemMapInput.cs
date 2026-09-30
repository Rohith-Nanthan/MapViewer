using UnityEngine;
using UnityEngine.InputSystem;

namespace Maps
{
    /// <summary>
    /// <see cref="IMapInput"/> backed by the Input System. Bindings live in an Input Actions asset, so they
    /// can be changed there, or rebound at runtime, without touching code.
    /// </summary>
    /// <remarks>
    /// <see cref="Enable"/> and <see cref="Disable"/> switch every referenced action on or off. Actions are
    /// only live while this component is enabled too, so disabling it never leaves them running. It only
    /// ever disables actions it enabled itself, so viewers sharing an actions asset do not cut each other off.
    /// </remarks>
    [AddComponentMenu("Map Viewer/Input System Map Input")]
    [DisallowMultipleComponent]
    public sealed class InputSystemMapInput : MonoBehaviour, IMapInput
    {
        [Tooltip("Directional pan (Vector2), e.g. left stick and D-pad.")]
        [SerializeField] InputActionReference pan;

        [Tooltip("Continuous zoom (Axis), positive zooms in, e.g. right trigger in and left trigger out.")]
        [SerializeField] InputActionReference zoom;

        [Tooltip("Stepped zoom (Axis), positive zooms in, e.g. the mouse scroll wheel.")]
        [SerializeField] InputActionReference zoomStep;

        [Tooltip("Hold to drag the map (Button), e.g. the middle mouse button.")]
        [SerializeField] InputActionReference drag;

        [Tooltip("Pointer position in screen pixels (Vector2), used to drag and to zoom towards the cursor.")]
        [SerializeField] InputActionReference point;

        bool _requested;

        public bool IsEnabled { get; private set; }

        public void Enable()
        {
            _requested = true;
            if (enabled && gameObject.activeInHierarchy)
                SetActionsEnabled(true);
        }

        public void Disable()
        {
            _requested = false;
            if (IsEnabled)
                SetActionsEnabled(false);
        }

        public MapInputFrame ReadFrame()
        {
            if (!IsEnabled)
                return default;

            InputAction dragAction = Resolve(drag);
            InputAction pointAction = Resolve(point);
            bool hasPointer = pointAction != null && pointAction.controls.Count > 0;

            return new MapInputFrame
            {
                Pan = Vector2.ClampMagnitude(Read<Vector2>(pan), 1f),
                Zoom = Mathf.Clamp(Read<float>(zoom), -1f, 1f),
                ZoomSteps = Read<float>(zoomStep),
                DragPressedThisFrame = dragAction != null && dragAction.WasPressedThisFrame(),
                DragHeld = dragAction != null && dragAction.IsPressed(),
                HasPointer = hasPointer,
                PointerPosition = hasPointer ? pointAction.ReadValue<Vector2>() : default,
            };
        }

        void OnEnable()
        {
            if (_requested)
                SetActionsEnabled(true);
        }

        void OnDisable()
        {
            if (IsEnabled)
                SetActionsEnabled(false);
        }

        void SetActionsEnabled(bool value)
        {
            SetEnabled(pan, value);
            SetEnabled(zoom, value);
            SetEnabled(zoomStep, value);
            SetEnabled(drag, value);
            SetEnabled(point, value);
            IsEnabled = value;
        }

        static void SetEnabled(InputActionReference reference, bool value)
        {
            InputAction action = Resolve(reference);
            if (action == null)
                return;

            if (value)
                action.Enable();
            else
                action.Disable();
        }

        static InputAction Resolve(InputActionReference reference) => reference != null ? reference.action : null;

        static T Read<T>(InputActionReference reference) where T : struct
        {
            InputAction action = Resolve(reference);
            return action != null ? action.ReadValue<T>() : default;
        }
    }
}
