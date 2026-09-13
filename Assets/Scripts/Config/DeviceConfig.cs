using NightCafe.Core;
using UnityEngine;

namespace NightCafe.Config
{
    /// <summary>
    /// The Bréve Deck as a physical object: how its buttons travel, how the lever slides, how
    /// the camera looks at it. The geometry itself comes from tools/shell_model.py; the numbers
    /// here are the ones worth nudging without rebuilding the model.
    /// </summary>
    [CreateAssetMenu(menuName = "NightCafe/Device Config", fileName = "DeviceConfig")]
    public sealed class DeviceConfig : ScriptableObject
    {
        [Header("Camera")]
        [Tooltip("Vertical field of view of the device camera; the framing distance follows from it.")]
        public float cameraFov = 24f;

        [Tooltip("Pitch towards the device in degrees - a little from above, like a handheld on a counter.")]
        public float cameraTiltDegrees = 10f;

        [Tooltip("Parallax: how far the viewpoint swings (degrees) as the phone tilts; 0 disables it.")]
        public float parallaxDegrees = 3f;

        [Header("Virtual buttons (GDD 4)")]
        [Tooltip("How far a cap sinks on a press, in device units.")]
        public float capTravel = 0.08f;

        [Tooltip("Seconds for the cap to go down, then to come back up.")]
        public float capPressSeconds = 0.04f;
        public float capReleaseSeconds = 0.09f;

        [Tooltip("The backlit cap fades out over this many seconds after the press.")]
        public float capLitFadeSeconds = 0.18f;

        [Header("Mode lever (GDD 5.1: A on the left, B on the right)")]
        [Tooltip("Knob offset from the slot centre; mode A sits at -x, mode B at +x.")]
        public float leverKnobX = 0.9f;

        [Tooltip("Seconds the knob takes to slide across, with a little overshoot.")]
        public float leverSlideSeconds = 0.12f;

        [Header("Attract mode (GDD 6)")]
        [Tooltip("Seconds of an untouched title screen before the demo starts.")]
        public float attractDelay = 8f;

        [Tooltip("Seconds an untouched game over screen stays before handing back to the title, where the lever and settings live.")]
        public float gameOverIdleSeconds = 6f;

        [Tooltip("Presses in the first moments of game over are ignored, so a tap already in flight cannot skip the result.")]
        public float gameOverRestartLockout = 0.8f;

        [Tooltip("Longest a demo runs before handing back to the title, even if the pilot is still alive.")]
        public float attractMaxDuration = 45f;

        [Tooltip("How long the pilot takes to react to a cup that needs a lane change.")]
        public float pilotReactionSeconds = 0.22f;

        [Tooltip("Share of cups the pilot deliberately lets drop, so the demo ends on its own.")]
        [Range(0f, 1f)] public float pilotFumbleChance = 0.08f;

        [Header("Brew timer (GDD 6) - tap the title clock to cycle 1..max minutes")]
        public int brewTimerMaxMinutes = 5;

        [Tooltip("Tap area of the clock in LCD units, like the title toggles")]
        public Vector2 clockHitSize = new(6f, 1.6f);

        [Header("LCD render texture")]
        [Tooltip("Pixels along the screen's height; width follows the 1272:892 proportion.")]
        public int lcdTextureHeight = 1122;
    }
}
