using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>
    /// Pure maths behind LcdPointer: a UV on the screen face (0..1, origin bottom-left, the
    /// way tools/shell_model.py authors it) becomes a point in the 2D LCD scene that the
    /// orthographic LCD camera frames.
    /// </summary>
    public static class LcdMapping
    {
        public static bool Inside(Vector2 uv) =>
            uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;

        /// <summary>
        /// The LCD camera looks along +Z at the 2D scene with its centre at `lcdCentre`; its
        /// view spans 2 * orthographicSize vertically and aspect times that horizontally.
        /// </summary>
        public static Vector2 LcdPointFromUv(Vector2 uv, Vector2 lcdCentre, float orthographicSize, float aspect)
        {
            float halfHeight = orthographicSize;
            float halfWidth = orthographicSize * aspect;
            return lcdCentre + new Vector2((uv.x - 0.5f) * 2f * halfWidth, (uv.y - 0.5f) * 2f * halfHeight);
        }
    }
}
