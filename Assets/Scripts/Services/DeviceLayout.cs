using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>
    /// Pure layout maths for the Bréve Deck. The device is a 3D model (tools/shell_model.py,
    /// whose BODY / LCD constants these mirror) seen by a perspective camera; the LCD content is
    /// a 2D scene rendered by an orthographic camera into a texture on the screen face.
    /// Units are Unity units = model units (about a centimetre).
    /// </summary>
    public static class DeviceLayout
    {
        /// <summary>Body 22 x 11 x 1.4, centred on the origin, top face towards the camera.</summary>
        public const float BodyHalfWidth = 11.0f;
        public const float BodyHalfHeight = 5.5f;
        public const float BodyThickness = 1.4f;

        /// <summary>The screen face: 1272:892 like screen_bg.png, its centre 0.30 above the body centre.</summary>
        public const float LcdWidth = 13.2f;
        public const float LcdHeight = 9.26f;
        public const float LcdCentreY = 0.30f;

        /// <summary>
        /// Orthographic half-height that maps screen_bg.png (892 px at 100 PPU) exactly onto the
        /// LCD render texture: one LCD unit is one world unit in the 2D scene, no scaling.
        /// </summary>
        public const float LcdOrthographicSize = 4.46f;

        /// <summary>Counter showing around the device at every aspect.</summary>
        public const float Margin = 0.35f;

        /// <summary>Camera pitch towards the device, degrees - "a little from above".</summary>
        public const float CameraTiltDegrees = 10f;

        /// <summary>
        /// Distance from the device centre at which the whole body fits the view with the margin:
        /// the height governs on phones (20:9 and wider), the width on tablets and the 16:9 editor.
        /// </summary>
        public static float CameraDistanceFor(float aspect, float verticalFovDegrees)
        {
            float halfTan = Mathf.Tan(0.5f * verticalFovDegrees * Mathf.Deg2Rad);
            float byHeight = (BodyHalfHeight + Margin) / halfTan;
            float byWidth = (BodyHalfWidth + Margin) / (halfTan * Mathf.Max(0.0001f, aspect));
            return Mathf.Max(byHeight, byWidth);
        }

        /// <summary>Invariant the tests lock down: the whole device is on screen at any sane aspect.</summary>
        public static bool DeviceFullyVisible(float aspect, float verticalFovDegrees)
        {
            float halfTan = Mathf.Tan(0.5f * verticalFovDegrees * Mathf.Deg2Rad);
            float distance = CameraDistanceFor(aspect, verticalFovDegrees);
            float halfHeight = distance * halfTan;
            float halfWidth = halfHeight * aspect;
            return halfHeight >= BodyHalfHeight && halfWidth >= BodyHalfWidth;
        }
    }
}
