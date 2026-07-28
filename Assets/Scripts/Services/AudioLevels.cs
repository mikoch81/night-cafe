using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>
    /// GDD 5.3 asks for the music to sit -18 LUFS relative to the SFX. True BS.1770 loudness
    /// metering is out of scope; since we author and peak-normalise both sides ourselves,
    /// a fixed decibel offset is a faithful stand-in.
    /// </summary>
    public static class AudioLevels
    {
        public static float LinearGain(float decibels) => Mathf.Pow(10f, decibels / 20f);
    }
}
