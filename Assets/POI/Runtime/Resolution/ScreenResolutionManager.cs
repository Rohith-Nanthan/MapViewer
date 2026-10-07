using System;
using UnityEngine;
using UnityEngine.Events;

namespace POI
{
    /// <summary>
    /// Tells other systems when the screen's resolution or safe area changes, e.g. after the window is resized,
    /// the device is rotated or the game moves to another display. Subscribe to <see cref="ResolutionChanged"/>.
    /// </summary>
    /// <remarks>
    /// Unity has no event for this, so the screen is checked once per frame. The check runs before other
    /// scripts' Update, so listeners can react within the same frame.
    /// </remarks>
    [AddComponentMenu("POI/Screen Resolution Manager")]
    [DefaultExecutionOrder(k_ExecutionOrder)]
    [DisallowMultipleComponent]
    public sealed class ScreenResolutionManager : MonoBehaviour
    {
        const int k_ExecutionOrder = -10000;

        [Tooltip("Write each change to the console, e.g. while testing layouts.")]
        [SerializeField] bool m_LogChanges;

        [Tooltip("Invoked after the screen's size or safe area changes, e.g. wired to POI UI's RefreshLayout.")]
        [SerializeField] UnityEvent m_OnResolutionChanged = new UnityEvent();

        ScreenResolutionTracker _tracker;

        /// <summary>Raised with the new resolution after the screen's size or safe area changes.</summary>
        public event Action<ScreenResolution> ResolutionChanged;

        /// <summary>The screen as of this frame's check.</summary>
        public ScreenResolution Current => Tracker.Current;

        public bool LogChanges
        {
            get => m_LogChanges;
            set => m_LogChanges = value;
        }

        public UnityEvent OnResolutionChanged => m_OnResolutionChanged;

        ScreenResolutionTracker Tracker => _tracker ??= new ScreenResolutionTracker(ScreenResolution.Current);

        /// <summary>Checks the screen now rather than at the start of the next frame.</summary>
        /// <returns>True when the resolution changed, in which case <see cref="ResolutionChanged"/> was raised.</returns>
        public bool Refresh()
        {
            if (!Tracker.Update(ScreenResolution.Current))
                return false;

            if (m_LogChanges)
                Debug.Log($"Screen resolution changed to {Current}.", this);

            ResolutionChanged?.Invoke(Current);
            m_OnResolutionChanged.Invoke();
            return true;
        }

        // The screen may have changed while this was disabled; report it on the first check.
        void OnEnable() => Refresh();

        void Update() => Refresh();
    }
}
