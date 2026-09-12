using NightCafe.Core;
using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Config
{
    /// <summary>
    /// Layout of the Bréve Deck shell: where the virtual buttons and the mode lever sit,
    /// and how the LCD screen is nested inside the shell cutout. Defaults are measured
    /// from the art, and live here so they can be nudged without editing code.
    /// </summary>
    [CreateAssetMenu(menuName = "NightCafe/Device Config", fileName = "DeviceConfig")]
    public sealed class DeviceConfig : ScriptableObject
    {
        [Header("Screen nesting (GDD 5.1 ScreenGlass)")]
        public float screenScale = DeviceLayout.ScreenScale;
        public float screenOffsetY = DeviceLayout.ScreenOffsetY;

        [Header("Virtual buttons (GDD 4) - right side; left mirrors X")]
        public Vector2 buttonUp = new(7.13f, 1.75f);
        public Vector2 buttonDown = new(7.13f, -0.87f);
        public float buttonScale = 0.3216f;

        [Tooltip("How long a button stays lit after a press, in seconds.")]
        public float buttonLitDuration = 0.10f;

        [Header("Mode lever (GDD 5.1: A on the left, B on the right)")]
        public Vector2 leverTrack = new(0f, -3.86f);
        public Vector2 leverKnob = new(-0.64f, -3.86f);
        public float leverTrackScale = 0.2961f;
        public float leverKnobScale = 0.2897f;

        [Tooltip("Tap area around the track that flips the mode on the title screen.")]
        public Vector2 leverHitSize = new(3.6f, 1.3f);

        [Header("Attract mode (GDD 6)")]
        [Tooltip("Seconds of an untouched title screen before the demo starts.")]
        public float attractDelay = 8f;

        [Tooltip("Seconds an untouched game over screen stays before handing back to the title, where the lever and settings live.")]
        public float gameOverIdleSeconds = 6f;

        [Tooltip("Longest a demo runs before handing back to the title, even if the pilot is still alive.")]
        public float attractMaxDuration = 45f;

        [Tooltip("How long the pilot takes to react to a cup that needs a lane change.")]
        public float pilotReactionSeconds = 0.22f;

        [Tooltip("Share of cups the pilot deliberately lets drop, so the demo ends on its own.")]
        [Range(0f, 1f)] public float pilotFumbleChance = 0.08f;

        [Header("Shell")]
        public float shellScale = 1f;

        public Vector2 ButtonPosition(LanePosition lane)
        {
            Vector2 right = lane.IsUp() ? buttonUp : buttonDown;
            return lane.IsLeft() ? new Vector2(-right.x, right.y) : right;
        }
    }
}
