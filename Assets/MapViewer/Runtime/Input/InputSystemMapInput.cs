using System.Collections.Generic;
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
    /// only live while this component is enabled too, so disabling it never leaves them running. Actions are
    /// reference counted, so viewers that share an actions asset never switch each other's input off.
    /// </remarks>
    [AddComponentMenu("Map Viewer/Input System Map Input")]
    [DisallowMultipleComponent]
    public sealed class InputSystemMapInput : MonoBehaviour, IMapInput
    {
        // How many map inputs currently use each action. An action stays enabled while anything uses it.
        static readonly Dictionary<InputAction, int> s_ActionUsers = new Dictionary<InputAction, int>();

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

        readonly List<InputAction> _acquiredActions = new List<InputAction>();
        bool _requested;

        public bool IsEnabled { get; private set; }

        public void Enable()
        {
            _requested = true;
            if (enabled && gameObject.activeInHierarchy)
                AcquireActions();
        }

        public void Disable()
        {
            _requested = false;
            ReleaseActions();
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
                AcquireActions();
        }

        void OnDisable() => ReleaseActions();

        void AcquireActions()
        {
            if (IsEnabled)
                return;

            Acquire(pan);
            Acquire(zoom);
            Acquire(zoomStep);
            Acquire(drag);
            Acquire(point);
            IsEnabled = true;
        }

        void Acquire(InputActionReference reference)
        {
            InputAction action = Resolve(reference);
            if (action == null || _acquiredActions.Contains(action))
                return;

            _acquiredActions.Add(action);
            s_ActionUsers.TryGetValue(action, out int users);
            s_ActionUsers[action] = users + 1;
            action.Enable();
        }

        void ReleaseActions()
        {
            foreach (InputAction action in _acquiredActions)
            {
                s_ActionUsers.TryGetValue(action, out int users);
                if (users > 1)
                {
                    s_ActionUsers[action] = users - 1;
                    continue;
                }

                s_ActionUsers.Remove(action);
                action.Disable();
            }

            _acquiredActions.Clear();
            IsEnabled = false;
        }

        // Static state survives entering Play Mode when domain reload is disabled, so start each session clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetActionUsers() => s_ActionUsers.Clear();

        static InputAction Resolve(InputActionReference reference) => reference != null ? reference.action : null;

        static T Read<T>(InputActionReference reference) where T : struct
        {
            InputAction action = Resolve(reference);
            return action != null ? action.ReadValue<T>() : default;
        }
    }
}
