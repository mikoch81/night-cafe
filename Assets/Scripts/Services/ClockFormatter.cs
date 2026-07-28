using System;

namespace NightCafe.Services
{
    /// <summary>
    /// The night clock on the title screen (GDD 6). The colon blinks, so the string must keep
    /// a constant width or the mono digits jitter.
    /// </summary>
    public static class ClockFormatter
    {
        public static string Format(DateTime time, bool colonVisible) =>
            $"{time.Hour:00}{(colonVisible ? ':' : ' ')}{time.Minute:00}";
    }
}
