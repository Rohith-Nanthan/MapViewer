using UnityEngine;

namespace POI
{
    /// <summary>
    /// A point of interest in the world. Drop it on any GameObject and assign an icon, then call
    /// <see cref="Activate"/> to show the icon on screen and on the compass, and <see cref="DeActivate"/> to
    /// hide it again.
    /// </summary>
    /// <remarks>
    /// The position is fixed in world space, so moving the GameObject afterwards does not move it. Every
    /// <see cref="POI_UI"/> picks active points of interest up through <see cref="POIRegistry"/>, so this
    /// component needs no reference to the UI. While its GameObject is disabled, an active point of interest
    /// is hidden; it shows again once the GameObject is enabled.
    /// </remarks>
    [AddComponentMenu("POI/POI World")]
    public sealed class POI_World : MonoBehaviour, IPointOfInterest
    {
        [Tooltip("Icon shown on screen and on the compass while active.")]
        [SerializeField] Sprite m_Icon;

        [Tooltip("World position the markers point at. It is fixed: moving this GameObject does not change it.")]
        [SerializeField] Vector3 m_Position;

        POIRegistry _registry;
        POIRegistry _registeredIn;
        bool _isActive;

        /// <summary>Icon shown on screen and on the compass. A change shows on the next frame.</summary>
        public Sprite Icon
        {
            get => m_Icon;
            set => m_Icon = value;
        }

        /// <summary>World position the markers point at.</summary>
        public Vector3 Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        /// <summary>Whether <see cref="Activate"/> was called more recently than <see cref="DeActivate"/>.</summary>
        public bool IsActive => _isActive;

        /// <summary>Where this point of interest is listed while active. Defaults to <see cref="POIRegistry.Default"/>.</summary>
        public POIRegistry Registry
        {
            get => _registry ?? POIRegistry.Default;
            set
            {
                Unregister();
                _registry = value;
                UpdateRegistration();
            }
        }

        /// <summary>Shows this point of interest on screen and on the compass.</summary>
        public void Activate()
        {
            _isActive = true;
            UpdateRegistration();
        }

        /// <summary>Hides this point of interest from the screen and the compass.</summary>
        public void DeActivate()
        {
            _isActive = false;
            UpdateRegistration();
        }

        void OnEnable() => UpdateRegistration();

        void OnDisable() => Unregister();

        // Dropping the component on an object starts it at that object's position.
        void Reset() => m_Position = transform.position;

        void UpdateRegistration()
        {
            if (_isActive && isActiveAndEnabled)
                Register();
            else
                Unregister();
        }

        void Register()
        {
            if (_registeredIn != null)
                return;

            _registeredIn = Registry;
            _registeredIn.Add(this);
        }

        void Unregister()
        {
            if (_registeredIn == null)
                return;

            POIRegistry registry = _registeredIn;
            _registeredIn = null;
            registry.Remove(this);
        }
    }
}
