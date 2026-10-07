using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace POI.Samples
{
    public sealed class POIDemoExample : MonoBehaviour
    {
        [Tooltip("Points the number keys toggle, in order: 1 toggles the first.")]
        [SerializeField] POI_World[] m_Points = Array.Empty<POI_World>();

        [Tooltip("Activate every point when the demo starts.")]
        [SerializeField] bool m_ActivateOnStart = true;

        [Tooltip("Optional text that lists the points and whether each is active.")]
        [SerializeField] TMP_Text m_Status;

        [SerializeField] InputAction m_TogglePoint = CreateButton("Toggle Point",
            "<Keyboard>/1", "<Keyboard>/2", "<Keyboard>/3", "<Keyboard>/4", "<Keyboard>/5",
            "<Keyboard>/6", "<Keyboard>/7", "<Keyboard>/8", "<Keyboard>/9");

        [SerializeField] InputAction m_ToggleAll = CreateButton("Toggle All", "<Keyboard>/0", "<Gamepad>/buttonWest");

        readonly StringBuilder _status = new StringBuilder();

        void Start()
        {
            if (m_ActivateOnStart)
                SetAll(true);
            else
                UpdateStatus();
        }

        void OnEnable()
        {
            m_TogglePoint.performed += OnTogglePoint;
            m_ToggleAll.performed += OnToggleAll;
            m_TogglePoint.Enable();
            m_ToggleAll.Enable();
        }

        void OnDisable()
        {
            m_TogglePoint.performed -= OnTogglePoint;
            m_ToggleAll.performed -= OnToggleAll;
            m_TogglePoint.Disable();
            m_ToggleAll.Disable();
        }

        void OnTogglePoint(InputAction.CallbackContext context)
        {
            if (!(context.control is KeyControl key))
                return;

            int index = key.keyCode - Key.Digit1;
            if (index >= 0 && index < m_Points.Length && m_Points[index] != null)
                Toggle(m_Points[index]);
            UpdateStatus();
        }

        // Turns everything off when all points are active, otherwise turns everything on.
        void OnToggleAll(InputAction.CallbackContext context)
        {
            bool allActive = true;
            foreach (POI_World point in m_Points)
                allActive &= point == null || point.IsActive;

            SetAll(!allActive);
        }

        void SetAll(bool active)
        {
            foreach (POI_World point in m_Points)
            {
                if (point == null)
                    continue;

                if (active)
                    point.Activate();
                else
                    point.DeActivate();
            }

            UpdateStatus();
        }

        static void Toggle(POI_World point)
        {
            if (point.IsActive)
                point.DeActivate();
            else
                point.Activate();
        }

        void UpdateStatus()
        {
            if (m_Status == null)
                return;

            _status.Clear();
            for (int i = 0; i < m_Points.Length; i++)
            {
                POI_World point = m_Points[i];
                if (point != null)
                    _status.Append(i + 1).Append("  ").Append(point.name).Append("  ").AppendLine(point.IsActive ? "on" : "off");
            }

            m_Status.text = _status.ToString();
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
