using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Audio
{
    public interface IHaptics
    {
        void OneShot(int milliseconds);
        void Pattern(long[] pattern);
    }

    public sealed class NullHaptics : IHaptics
    {
        public void OneShot(int milliseconds) { }
        public void Pattern(long[] pattern) { }
    }

    /// <summary>
    /// Android vibration with real durations (GDD 4: 15 / 60 / 3x80 ms).
    /// Handheld.Vibrate is useless here - it ignores duration and always fires ~500 ms - so this
    /// goes through the platform Vibrator via JNI. The VIBRATE permission is injected by
    /// AndroidManifestPostProcessor, because pure JNI does not make Unity add it automatically.
    /// </summary>
    public sealed class AndroidHaptics : IHaptics
    {
        readonly AndroidJavaObject _vibrator;
        readonly int _sdkInt;

        public AndroidHaptics()
        {
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            _sdkInt = version.GetStatic<int>("SDK_INT");

            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");

            if (_sdkInt >= 31)
            {
                using AndroidJavaObject manager =
                    activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                _vibrator = manager?.Call<AndroidJavaObject>("getDefaultVibrator");
            }
            else
            {
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
        }

        public bool IsAvailable => _vibrator != null && _vibrator.Call<bool>("hasVibrator");

        public void OneShot(int milliseconds)
        {
            if (_vibrator == null || milliseconds <= 0)
                return;

            if (_sdkInt >= 26)
            {
                using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                int defaultAmplitude = effectClass.GetStatic<int>("DEFAULT_AMPLITUDE");
                using AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                    "createOneShot", (long)milliseconds, defaultAmplitude);
                _vibrator.Call("vibrate", effect);
            }
            else
            {
                _vibrator.Call("vibrate", (long)milliseconds);
            }
        }

        public void Pattern(long[] pattern)
        {
            if (_vibrator == null || pattern == null || pattern.Length == 0)
                return;

            if (_sdkInt >= 26)
            {
                using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                using AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                    "createWaveform", pattern, -1);
                _vibrator.Call("vibrate", effect);
            }
            else
            {
                _vibrator.Call("vibrate", pattern, -1);
            }
        }
    }

    /// <summary>Routes game events to the platform, respecting the player's toggle.</summary>
    public sealed class HapticsService
    {
        readonly IHaptics _backend;
        readonly SettingsService _settings;

        public HapticsService(IHaptics backend, SettingsService settings)
        {
            _backend = backend;
            _settings = settings;
        }

        /// <summary>Attract mode plays silently: the phone must not buzz on the title screen.</summary>
        public bool Muted { get; set; }

        bool Enabled => !Muted && (_settings == null || _settings.HapticsEnabled);

        public void OneShot(int milliseconds)
        {
            if (Enabled)
                _backend.OneShot(milliseconds);
        }

        public void Pattern(long[] pattern)
        {
            if (Enabled)
                _backend.Pattern(pattern);
        }

        public static IHaptics CreateBackend()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var android = new AndroidHaptics();
                if (android.IsAvailable)
                    return android;

                Debug.Log("[NightCafe] No vibrator on this device.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NightCafe] Haptics unavailable: {e.Message}");
            }
#endif
            return new NullHaptics();
        }
    }
}
