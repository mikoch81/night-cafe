namespace NightCafe.Core
{
    /// <summary>
    /// Sorting layer names from GDD 5.1, back to front.
    /// Created in ProjectSettings/TagManager.asset by the setup script.
    /// </summary>
    public static class SortingLayers
    {
        public const string DeviceShell = "DeviceShell";
        public const string ScreenGlass = "ScreenGlass";
        public const string Segments = "Segments";
        public const string ScreenFx = "ScreenFX";
        public const string Hud = "HUD_TMP";

        /// <summary>In GDD order; the setup script creates them exactly like this.</summary>
        public static readonly string[] InOrder =
        {
            DeviceShell,
            ScreenGlass,
            Segments,
            ScreenFx,
            Hud
        };
    }
}
