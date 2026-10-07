using System;
using UnityEngine;
using UnityEngine.Events;

namespace POI.Demo
{
    [AddComponentMenu("POI Demo/Screen Resolution Manager")]
    [DefaultExecutionOrder(k_ExecutionOrder)]
    [DisallowMultipleComponent]
    public sealed class ScreenResolutionManager : MonoBehaviour
    {
        // Unity has no resolution-changed event, so the screen is checked every frame, before other scripts'
        // Update, so listeners can react within the same frame.
        const int k_ExecutionOrder = -10000;

        [Tooltip("Write each change to the console, e.g. while testing layouts.")]
        [SerializeField] bool m_LogChanges;

        [Tooltip("Invoked after the screen's size or safe area changes, e.g. wired to POI UI's RefreshLayout.")]
        [SerializeField] UnityEvent m_OnResolutionChanged = new UnityEvent();

        ScreenResolutionTracker _tracker;

        public event Action<ScreenResolution> ResolutionChanged;

        public ScreenResolution Current => Tracker.Current;

        public bool LogChanges
        {
            get => m_LogChanges;
            set => m_LogChanges = value;
        }

        public UnityEvent OnResolutionChanged => m_OnResolutionChanged;

        ScreenResolutionTracker Tracker => _tracker ??= new ScreenResolutionTracker(ScreenResolution.Current);

        public bool CheckForResolutionChange()
        {
            if (!Tracker.TryUpdate(ScreenResolution.Current))
                return false;

            if (m_LogChanges)
                Debug.Log($"Screen resolution changed to {Current}.", this);

            ResolutionChanged?.Invoke(Current);
            m_OnResolutionChanged.Invoke();
            return true;
        }

        // The screen may have changed while this was disabled.
        void OnEnable() => CheckForResolutionChange();

        void Update() => CheckForResolutionChange();
    }
}
