using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Maps.Samples
{
    /// <summary>
    /// Example of hosting a map in a pause menu. The menu's button opens the map (wired to
    /// <see cref="MapViewer.Enable"/> in the Inspector), these actions close or toggle it, and the menu
    /// hides while the map is open.
    /// </summary>
    public sealed class MapMenuExample : MonoBehaviour
    {
        [SerializeField] MapViewer m_MapViewer;

        [Tooltip("Menu content hidden while the map is open.")]
        [SerializeField] GameObject m_Menu;

        [Tooltip("Selected when the menu shows, so a gamepad can open the map again.")]
        [SerializeField] Selectable m_FirstSelected;

        [SerializeField] InputAction m_CloseMap = CreateButton("Close Map", "<Keyboard>/escape", "<Gamepad>/buttonEast");
        [SerializeField] InputAction m_ToggleMap = CreateButton("Toggle Map", "<Keyboard>/m", "<Gamepad>/select");

        IMapViewer Map => m_MapViewer;

        void OnEnable()
        {
            Map.EnabledChanged += OnMapEnabledChanged;
            m_CloseMap.performed += OnCloseMap;
            m_ToggleMap.performed += OnToggleMap;
            m_CloseMap.Enable();
            m_ToggleMap.Enable();
            OnMapEnabledChanged(Map.IsEnabled);
        }

        void OnDisable()
        {
            Map.EnabledChanged -= OnMapEnabledChanged;
            m_CloseMap.performed -= OnCloseMap;
            m_ToggleMap.performed -= OnToggleMap;
            m_CloseMap.Disable();
            m_ToggleMap.Disable();
        }

        void OnCloseMap(InputAction.CallbackContext context)
        {
            if (Map.IsEnabled)
                Map.Disable();
        }

        void OnToggleMap(InputAction.CallbackContext context) => m_MapViewer.Toggle();

        void OnMapEnabledChanged(bool mapEnabled)
        {
            if (m_Menu != null)
                m_Menu.SetActive(!mapEnabled);

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(!mapEnabled && m_FirstSelected != null ? m_FirstSelected.gameObject : null);
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
