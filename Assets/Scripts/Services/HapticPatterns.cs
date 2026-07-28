namespace NightCafe.Services
{
    /// <summary>
    /// Android vibration timings (GDD 4). Android's waveform API takes alternating
    /// off/on durations starting with an off period, hence the leading zero.
    /// </summary>
    public static class HapticPatterns
    {
        /// <summary>
        /// Builds {0, on, gap, on, gap, on...} for the requested number of pulses.
        /// One pulse collapses to {0, on} so it can go through the cheaper one-shot path.
        /// </summary>
        public static long[] Pulses(int count, long onMilliseconds, long gapMilliseconds)
        {
            if (count <= 0)
                return new long[0];

            var pattern = new long[count * 2];
            pattern[0] = 0;
            pattern[1] = onMilliseconds;

            for (int i = 1; i < count; i++)
            {
                pattern[i * 2] = gapMilliseconds;
                pattern[i * 2 + 1] = onMilliseconds;
            }

            return pattern;
        }
    }
}
