namespace Maps
{
    /// <summary>How the map is sized inside the viewport at a zoom of 1.</summary>
    public enum MapFitMode
    {
        /// <summary>The whole map is visible. Empty space may show along one axis.</summary>
        Fit = 0,

        /// <summary>The map covers the whole viewport. Part of it may be cropped along one axis.</summary>
        Fill = 1,
    }
}
