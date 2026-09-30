using System;
using UnityEngine;
using UnityEngine.Events;

namespace Maps
{
    /// <summary>Inspector-assignable callbacks for a <see cref="MapViewer"/>, e.g. to hide menu buttons.</summary>
    [Serializable]
    public sealed class MapViewerEvents
    {
        [Tooltip("Invoked after the map is enabled.")]
        [SerializeField] UnityEvent m_OnEnabled = new UnityEvent();

        [Tooltip("Invoked after the map is disabled.")]
        [SerializeField] UnityEvent m_OnDisabled = new UnityEvent();

        public UnityEvent OnEnabled => m_OnEnabled;

        public UnityEvent OnDisabled => m_OnDisabled;
    }
}
