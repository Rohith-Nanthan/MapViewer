namespace POI
{
    public sealed class ScreenResolutionTracker
    {
        public ScreenResolutionTracker(ScreenResolution initial)
        {
            Current = initial;
        }

        public ScreenResolution Current { get; private set; }

        // Returns true when the reading differs from the current one, which it then replaces.
        public bool TryUpdate(ScreenResolution reading)
        {
            if (reading == Current)
                return false;

            Current = reading;
            return true;
        }
    }
}
