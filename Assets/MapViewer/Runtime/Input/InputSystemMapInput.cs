using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Maps
{
    /// <summary>
    /// <see cref="IMapInput"/> backed by the Input System and configured on <see cref="MapViewer"/>. Bindings
    /// live in an Input Actions asset, so they can be changed there, or rebound at runtime, without code.
    /// </summary>
    /// <remarks>
    /// <see cref="Enable"/> and <see cref="Disable"/> switch every referenced action on or off. Actions are
    /// reference counted, so viewers that share an actions asset never switch each other's input off.
    /// </remarks>
    [Serializable]
    public sealed class InputSystemMapInput : IMapInput
    {
        // How many map inputs currently use each action. An action stays enabled while anything uses it.
        static readonly Dictionary<InputAction, int> s_ActionUsers = new Dictionary<InputAction, int>();

        [Tooltip("Directional pan (Vector2), e.g. left stick and D-pad.")]
        [SerializeField] InputActionReference m_Pan;

        [Tooltip("Continuous zoom (Axis), positive zooms in, e.g. right trigger in and left trigger out.")]
        [SerializeField] InputActionReference m_Zoom;

        [Tooltip("Stepped zoom (Axis), positive zooms in, e.g. the mouse scroll wheel.")]
        [SerializeField] InputActionReference m_ZoomStep;

        [Tooltip("Hold to drag the map (Button), e.g. the middle mouse button.")]
        [SerializeField] InputActionReference m_Drag;

        [Tooltip("Pointer position in screen pixels (Vector2), used to drag and to zoom towards the cursor.")]
        [SerializeField] InputActionReference m_Point;

        List<InputAction> _acquiredActions;

        public InputSystemMapInput()
        {
        }

        public InputSystemMapInput(InputActionReference pan, InputActionReference zoom, InputActionReference zoomStep,
            InputActionReference drag, InputActionReference point)
        {
            m_Pan = pan;
            m_Zoom = zoom;
            m_ZoomStep = zoomStep;
            m_Drag = drag;
            m_Point = point;
        }

        public bool IsEnabled { get; private set; }

        public void Enable()
        {
            if (IsEnabled)
                return;

            _acquiredActions ??= new List<InputAction>();
            Acquire(m_Pan);
            Acquire(m_Zoom);
            Acquire(m_ZoomStep);
            Acquire(m_Drag);
            Acquire(m_Point);
            IsEnabled = true;
        }

        public void Disable()
        {
            if (!IsEnabled)
                return;

            foreach (InputAction action in _acquiredActions)
                Release(action);
            _acquiredActions.Clear();
            IsEnabled = false;
        }

        public MapInputFrame ReadFrame()
        {
            if (!IsEnabled)
                return default;

            InputAction dragAction = Resolve(m_Drag);
            InputAction pointAction = Resolve(m_Point);
            bool hasPointer = pointAction != null && pointAction.controls.Count > 0;

            return new MapInputFrame
            {
                Pan = Vector2.ClampMagnitude(Read<Vector2>(m_Pan), 1f),
                Zoom = Mathf.Clamp(Read<float>(m_Zoom), -1f, 1f),
                ZoomSteps = Read<float>(m_ZoomStep),
                DragPressedThisFrame = dragAction != null && dragAction.WasPressedThisFrame(),
                DragHeld = dragAction != null && dragAction.IsPressed(),
                HasPointer = hasPointer,
                PointerPosition = hasPointer ? pointAction.ReadValue<Vector2>() : default,
            };
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

        static void Release(InputAction action)
        {
            s_ActionUsers.TryGetValue(action, out int users);
            if (users > 1)
            {
                s_ActionUsers[action] = users - 1;
                return;
            }

            s_ActionUsers.Remove(action);
            action.Disable();
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
