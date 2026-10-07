using UnityEngine;

namespace POI.Demo
{
    [AddComponentMenu("POI Demo/POI World")]
    public sealed class POI_World : MonoBehaviour, IPointOfInterest
    {
        [Tooltip("Icon shown on screen and on the compass while active.")]
        [SerializeField] Sprite m_Icon;

        [Tooltip("World position the markers point at. It is fixed: moving this GameObject does not change it.")]
        [SerializeField] Vector3 m_Position;

        bool _isActive;

        public Sprite Icon
        {
            get => m_Icon;
            set => m_Icon = value;
        }

        public Vector3 Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public bool IsActive => _isActive;

        public void Activate()
        {
            _isActive = true;
            if (isActiveAndEnabled)
                POIRegistry.Default.Add(this);
        }

        public void DeActivate()
        {
            _isActive = false;
            POIRegistry.Default.Remove(this);
        }

        void OnEnable()
        {
            if (_isActive)
                POIRegistry.Default.Add(this);
        }

        void OnDisable() => POIRegistry.Default.Remove(this);

        // Dropping the component on an object starts it at that object's position.
        void Reset() => m_Position = transform.position;
    }
}
