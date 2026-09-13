using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>
    /// Pure layout maths for the Bréve Deck shell. All values are world units with the
    /// shell canvas centred on the origin at 100 pixels per unit; they mirror the BODY / LCD
    /// constants in tools/shell_render.py, which renders device_shell.png and screen_bg.png fits.
    /// </summary>
    public static class DeviceLayout
    {
        /// <summary>Half width of the chassis (body 2200 px on a 2400 px canvas; the rest is drop shadow).</summary>
        public const float ShellContentHalfWidth = 11.00f;

        /// <summary>Half height of the chassis (960 px tall): 20:9 like the phones it is played on.</summary>
        public const float ShellContentHalfHeight = 4.80f;

        /// <summary>Counter showing around the device at every aspect.</summary>
        public const float Margin = 0.25f;

        /// <summary>LCD cutout: 1028x712 px centred 35 px above the shell centre.</summary>
        public const float LcdHalfWidth = 5.14f;
        public const float LcdTop = 3.91f;
        public const float LcdBottom = -3.21f;

        /// <summary>screen_bg.png (1272x892) scaled to fill the 1028x712 cutout by height.</summary>
        public const float ScreenScale = 0.79821f;

        /// <summary>The cutout centre sits 35 px above the shell centre.</summary>
        public const float ScreenOffsetY = 0.35f;

        /// <summary>
        /// Orthographic size that shows the whole device with a margin at any aspect: the height
        /// governs on phones (20:9 and wider), the width on tablets and the 16:9 editor view.
        /// </summary>
        public static float OrthographicSizeFor(float aspect)
        {
            float fitWidth = (ShellContentHalfWidth + Margin) / Mathf.Max(0.0001f, aspect);
            return Mathf.Max(ShellContentHalfHeight + Margin, fitWidth);
        }

        /// <summary>Invariant the tests lock down: the whole device is on screen at any sane aspect.</summary>
        public static bool DeviceFullyVisible(float aspect)
        {
            float orthoSize = OrthographicSizeFor(aspect);
            float halfWidth = orthoSize * aspect;

            return orthoSize >= ShellContentHalfHeight
                   && halfWidth >= ShellContentHalfWidth;
        }
    }
}
