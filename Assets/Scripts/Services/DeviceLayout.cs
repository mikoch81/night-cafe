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
        /// <summary>Body 22 x 11 x 1.8, centred on the origin, top face towards the camera.</summary>
        public const float BodyHalfWidth = 11.0f;
        public const float BodyHalfHeight = 5.5f;
        public const float BodyThickness = 1.8f;

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

        /// <summary>
        /// Camera pitch towards the device, degrees. The camera sits on the near side (the bottom
        /// edge of the screen, where the player is) and looks a little down, so the front wall
        /// and the shadow under it show below the body - a handheld lying on the counter.
        /// </summary>
        public const float CameraTiltDegrees = 14f;

        /// <summary>
        /// Camera position relative to the slab centre for a tilt and a distance: on the near
        /// side (-y), in front of the top face (-z).
        /// </summary>
        public static Vector3 CameraOffset(float tiltDegrees, float distance)
        {
            float tilt = tiltDegrees * Mathf.Deg2Rad;
            return new Vector3(0f, -Mathf.Sin(tilt), -Mathf.Cos(tilt)) * distance;
        }

        /// <summary>
        /// Distance from the slab centre at which the whole body, walls included, fits the view
        /// with the margin: the height governs on phones (20:9 and wider), the width on tablets
        /// and the 16:9 editor. Every corner of the slab is checked, because which one is
        /// critical depends on the tilt: nearly straight on it is the top face's corners (closest
        /// to the camera), tilted it is the bottom face's near edge (lowest on screen).
        /// </summary>
        public static float CameraDistanceFor(float aspect, float verticalFovDegrees, float tiltDegrees = CameraTiltDegrees)
        {
            float halfTan = Mathf.Tan(0.5f * verticalFovDegrees * Mathf.Deg2Rad);
            float tilt = tiltDegrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(tilt), cos = Mathf.Cos(tilt);
            float distance = 0f;

            for (int i = 0; i < 8; i++)
            {
                Corner(i, out float x, out float y, out float z);
                // Camera forward is (0, sin, cos), camera up (0, cos, -sin): see CameraOffset.
                float nearer = -(y * sin + z * cos);            // how much closer than the slab centre
                float vertical = Mathf.Abs(y * cos - z * sin);
                distance = Mathf.Max(distance, (vertical + Margin) / halfTan + nearer);
                distance = Mathf.Max(distance, (Mathf.Abs(x) + Margin) / (halfTan * Mathf.Max(0.0001f, aspect)) + nearer);
            }

            return distance;
        }

        /// <summary>Invariant the tests lock down: every corner of the slab is on screen at any sane aspect.</summary>
        public static bool DeviceFullyVisible(float aspect, float verticalFovDegrees, float tiltDegrees = CameraTiltDegrees)
        {
            float halfTan = Mathf.Tan(0.5f * verticalFovDegrees * Mathf.Deg2Rad);
            float distance = CameraDistanceFor(aspect, verticalFovDegrees, tiltDegrees);
            float tilt = tiltDegrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(tilt), cos = Mathf.Cos(tilt);

            for (int i = 0; i < 8; i++)
            {
                Corner(i, out float x, out float y, out float z);
                float depth = distance + y * sin + z * cos;
                float vertical = y * cos - z * sin;
                float halfHeight = depth * halfTan;
                if (Mathf.Abs(vertical) > halfHeight || Mathf.Abs(x) > halfHeight * aspect)
                    return false;
            }

            return true;
        }

        /// <summary>The slab's corners relative to its centre; z negative = top face (towards the camera).</summary>
        static void Corner(int i, out float x, out float y, out float z)
        {
            x = (i & 1) == 0 ? -BodyHalfWidth : BodyHalfWidth;
            y = (i & 2) == 0 ? -BodyHalfHeight : BodyHalfHeight;
            z = (i & 4) == 0 ? -BodyThickness * 0.5f : BodyThickness * 0.5f;
        }
    }
}
