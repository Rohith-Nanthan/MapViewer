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
        [SerializeField] MapViewer mapViewer;

        [Tooltip("Menu content hidden while the map is open.")]
        [SerializeField] GameObject menu;

        [Tooltip("Selected when the menu shows, so a gamepad can open the map again.")]
        [SerializeField] Selectable firstSelected;

        [SerializeField] InputAction closeMap = CreateButton("Close Map", "<Keyboard>/escape", "<Gamepad>/buttonEast");
        [SerializeField] InputAction toggleMap = CreateButton("Toggle Map", "<Keyboard>/m", "<Gamepad>/select");

        IMapViewer Map => mapViewer;

        void OnEnable()
        {
            Map.EnabledChanged += OnMapEnabledChanged;
            closeMap.performed += OnCloseMap;
            toggleMap.performed += OnToggleMap;
            closeMap.Enable();
            toggleMap.Enable();
            OnMapEnabledChanged(Map.IsEnabled);
        }

        void OnDisable()
        {
            Map.EnabledChanged -= OnMapEnabledChanged;
            closeMap.performed -= OnCloseMap;
            toggleMap.performed -= OnToggleMap;
            closeMap.Disable();
            toggleMap.Disable();
        }

        void OnCloseMap(InputAction.CallbackContext context)
        {
            if (Map.IsEnabled)
                Map.Disable();
        }

        void OnToggleMap(InputAction.CallbackContext context) => mapViewer.Toggle();

        void OnMapEnabledChanged(bool mapEnabled)
        {
            if (menu != null)
                menu.SetActive(!mapEnabled);

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(!mapEnabled && firstSelected != null ? firstSelected.gameObject : null);
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
