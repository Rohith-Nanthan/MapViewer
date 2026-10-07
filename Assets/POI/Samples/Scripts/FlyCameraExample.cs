using UnityEngine;
using UnityEngine.InputSystem;

namespace POI.Samples
{
    // Sprinting moves several times faster, and the spin turns a full circle every second, to try the markers
    // against a fast camera.
    public sealed class FlyCameraExample : MonoBehaviour
    {
        const float k_MaxPitch = 89f;

        [Tooltip("Movement speed in meters per second.")]
        [SerializeField, Min(0f)] float m_MoveSpeed = 10f;

        [Tooltip("Speed multiplier while sprinting.")]
        [SerializeField, Min(1f)] float m_SprintMultiplier = 6f;

        [Tooltip("Degrees turned per pixel of mouse movement while the look button is held.")]
        [SerializeField, Min(0f)] float m_MouseSensitivity = 0.15f;

        [Tooltip("Degrees per second at full right-stick deflection.")]
        [SerializeField, Min(0f)] float m_StickLookSpeed = 140f;

        [Tooltip("Degrees per second the camera turns while spinning.")]
        [SerializeField] float m_SpinSpeed = 360f;

        [SerializeField] InputAction m_Move = CreateMove();
        [SerializeField] InputAction m_Elevate = CreateElevate();
        [SerializeField] InputAction m_MouseLook = new InputAction("Mouse Look", InputActionType.Value, "<Mouse>/delta");
        [SerializeField] InputAction m_MouseLookHold = CreateButton("Mouse Look Hold", "<Mouse>/rightButton");
        [SerializeField] InputAction m_StickLook = new InputAction("Stick Look", InputActionType.Value, "<Gamepad>/rightStick");
        [SerializeField] InputAction m_Sprint = CreateButton("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
        [SerializeField] InputAction m_ToggleSpin = CreateButton("Toggle Spin", "<Keyboard>/space", "<Gamepad>/buttonNorth");

        float _yaw;
        float _pitch;
        bool _isSpinning;

        void Awake()
        {
            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = Mathf.DeltaAngle(0f, angles.x);
        }

        void OnEnable()
        {
            m_ToggleSpin.performed += OnToggleSpin;
            foreach (InputAction action in Actions)
                action.Enable();
        }

        void OnDisable()
        {
            m_ToggleSpin.performed -= OnToggleSpin;
            foreach (InputAction action in Actions)
                action.Disable();
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;

            if (m_MouseLookHold.IsPressed())
            {
                Vector2 mouse = m_MouseLook.ReadValue<Vector2>() * m_MouseSensitivity;
                _yaw += mouse.x;
                _pitch -= mouse.y;
            }

            Vector2 stick = m_StickLook.ReadValue<Vector2>() * (m_StickLookSpeed * deltaTime);
            _yaw += stick.x;
            _pitch -= stick.y;
            if (_isSpinning)
                _yaw += m_SpinSpeed * deltaTime;

            _pitch = Mathf.Clamp(_pitch, -k_MaxPitch, k_MaxPitch);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            Vector2 move = m_Move.ReadValue<Vector2>();
            Vector3 direction = transform.right * move.x + transform.forward * move.y + Vector3.up * m_Elevate.ReadValue<float>();
            float speed = m_MoveSpeed * (m_Sprint.IsPressed() ? m_SprintMultiplier : 1f);
            transform.position += Vector3.ClampMagnitude(direction, 1f) * (speed * deltaTime);
        }

        InputAction[] Actions => new[] { m_Move, m_Elevate, m_MouseLook, m_MouseLookHold, m_StickLook, m_Sprint, m_ToggleSpin };

        void OnToggleSpin(InputAction.CallbackContext context) => _isSpinning = !_isSpinning;

        static InputAction CreateMove()
        {
            var action = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            action.AddBinding("<Gamepad>/leftStick");
            return action;
        }

        static InputAction CreateElevate()
        {
            var action = new InputAction("Elevate", InputActionType.Value, expectedControlType: "Axis");
            action.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            action.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/leftShoulder").With("Positive", "<Gamepad>/rightShoulder");
            return action;
        }

        static InputAction CreateButton(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (string binding in bindings)
                action.AddBinding(binding);
            return action;
        }
    }
}
