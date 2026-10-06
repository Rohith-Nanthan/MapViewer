namespace POI
{
    /// <summary>
    /// Detects screen resolution changes from successive readings. <see cref="ScreenResolutionManager"/> feeds
    /// it a reading every frame; as a plain object it can also be polled from anywhere else, or from a test.
    /// </summary>
    public sealed class ScreenResolutionTracker
    {
        public ScreenResolutionTracker(ScreenResolution initial)
        {
            Current = initial;
        }

        /// <summary>The latest reading.</summary>
        public ScreenResolution Current { get; private set; }

        /// <summary>Records a reading.</summary>
        /// <returns>True when it differs from the previous one.</returns>
        public bool Update(ScreenResolution reading)
        {
            if (reading == Current)
                return false;

            Current = reading;
            return true;
        }
    }
}
