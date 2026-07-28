using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>
    /// Sablé's walk across the bottom of the LCD (GDD 2.5): 1.6 s, two frames, fading in and out
    /// at the edges so the cat does not pop.
    /// </summary>
    public static class CatPath
    {
        public static float XAt(float t01, float startX, float endX) =>
            Mathf.LerpUnclamped(startX, endX, Mathf.Clamp01(t01));

        /// <summary>Alternates 0/1 at the given rate; a two-frame mop shuffle.</summary>
        public static int FrameIndexAt(float elapsedSeconds, float framesPerSecond) =>
            Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * framesPerSecond) % 2;

        /// <summary>0 at both ends, 1 across the middle - fades over the first and last tenth.</summary>
        public static float EdgeAlpha(float t01)
        {
            const float fade = 0.1f;
            float t = Mathf.Clamp01(t01);

            if (t < fade)
                return t / fade;

            if (t > 1f - fade)
                return (1f - t) / fade;

            return 1f;
        }
    }
}
