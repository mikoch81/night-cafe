using UnityEngine;

namespace NightCafe.Config
{
    /// <summary>
    /// The Neo-LCD palette (MANIFEST.txt, GDD 5.2) in one place. The scene generator reads it
    /// when it builds views and pushes the colours into their serialised fields, so the amber
    /// is authored once instead of being retyped in every view and every setup method.
    /// </summary>
    [CreateAssetMenu(menuName = "NightCafe/Palette Config", fileName = "PaletteConfig")]
    public sealed class PaletteConfig : ScriptableObject
    {
        [Tooltip("#ffc966 - lit segment")]
        public Color activeAmber = new(1f, 0.788f, 0.4f);

        [Tooltip("#ffd27a - the brightest highlight: cups, clock")]
        public Color brightAmber = new(1f, 0.824f, 0.478f);

        [Tooltip("#3a2c18 - unlit segment")]
        public Color inactiveAmber = new(0.227f, 0.173f, 0.094f);

        [Tooltip("#120d09 - LCD glass")]
        public Color glassBlack = new(0.071f, 0.051f, 0.035f);

        [Tooltip("#1a1412 - the counter the device lies on: camera clear colour around the shell")]
        public Color woodBackground = new(0.102f, 0.078f, 0.071f);

        [Header("Glass over the LCD (NightCafe/LcdGlass)")]
        [Tooltip("Inner shadow where the bezel meets the display; 0 = flat glass")]
        [Range(0f, 1f)] public float glassVignette = 0.55f;

        [Tooltip("How fast the shadow falls off towards the centre")]
        [Range(0.5f, 8f)] public float glassVignetteFalloff = 3f;

        [Tooltip("One soft reflection blob near the top-left; keep it barely there")]
        [Range(0f, 0.3f)] public float glassGlare = 0.05f;
    }
}
