using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>
    /// Pure layout maths for the Bréve Deck shell. All values are world units with the
    /// shell canvas centred on the origin at 100 pixels per unit, measured from
    /// Assets/Art/device/device_shell.png and Assets/Art/screen/screen_bg.png.
    /// </summary>
    public static class DeviceLayout
    {
        /// <summary>Half width of the painted wood (canvas is 9.60, but 20 px each side is transparent).</summary>
        public const float ShellContentHalfWidth = 9.40f;

        /// <summary>Half height of the painted wood; the camera never reveals more than this.</summary>
        public const float ShellContentHalfHeight = 5.10f;

        /// <summary>LCD cutout edges: x 446..1474, y 136..848 of a 1920x1080 canvas.</summary>
        public const float LcdHalfWidth = 5.14f;
        public const float LcdTop = 4.04f;
        public const float LcdBottom = -3.08f;

        /// <summary>screen_bg.png (1272x892) scaled to fill the 1028x712 cutout by height.</summary>
        public const float ScreenScale = 0.79821f;

        /// <summary>The cutout centre sits 48 px above the shell centre.</summary>
        public const float ScreenOffsetY = 0.48f;

        /// <summary>
        /// Orthographic size that fills the screen width with wood while keeping the whole LCD visible.
        /// The lower clamp guarantees the LCD top edge (4.04) stays on screen on very wide phones;
        /// the upper clamp stops the camera from showing past the wood on tall ones.
        /// </summary>
        public static float OrthographicSizeFor(float aspect)
        {
            float fitWidth = ShellContentHalfWidth / Mathf.Max(0.0001f, aspect);
            return Mathf.Clamp(fitWidth, 4.10f, ShellContentHalfHeight);
        }

        /// <summary>Invariant the tests lock down: the LCD is never cropped at any sane aspect.</summary>
        public static bool LcdFullyVisible(float aspect)
        {
            float orthoSize = OrthographicSizeFor(aspect);
            float halfWidth = orthoSize * aspect;

            return orthoSize >= LcdTop
                   && orthoSize >= -LcdBottom
                   && halfWidth >= LcdHalfWidth;
        }
    }
}
