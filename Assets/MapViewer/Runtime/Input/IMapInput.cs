namespace Maps
{
    /// <summary>
    /// Source of map input. The map only depends on this contract, so another input system, a
    /// replay or an automated test can drive it without changing the map itself.
    /// </summary>
    public interface IMapInput
    {
        /// <summary>Whether the source is currently listening to input.</summary>
        bool IsEnabled { get; }

        /// <summary>Starts listening to input, e.g. by enabling the underlying input actions.</summary>
        void Enable();

        /// <summary>Stops listening to input.</summary>
        void Disable();

        /// <summary>Reads this frame's input. Returns an empty frame while disabled.</summary>
        MapInputFrame ReadFrame();
    }
}
