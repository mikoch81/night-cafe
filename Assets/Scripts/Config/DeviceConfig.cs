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

        [Header("Mode lever (decorative until Mode B ships)")]
        public Vector2 leverTrack = new(0f, -3.86f);
        public Vector2 leverKnob = new(-0.64f, -3.86f);
        public float leverTrackScale = 0.2961f;
        public float leverKnobScale = 0.2897f;

        [Header("Shell")]
        public float shellScale = 1f;

        public Vector2 ButtonPosition(LanePosition lane)
        {
            Vector2 right = lane.IsUp() ? buttonUp : buttonDown;
            return lane.IsLeft() ? new Vector2(-right.x, right.y) : right;
        }
    }
}
