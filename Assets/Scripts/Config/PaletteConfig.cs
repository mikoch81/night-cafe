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

        [Tooltip("#54301a - wood behind the shell, also the camera clear colour")]
        public Color woodBackground = new(0.329f, 0.188f, 0.102f);
    }
}
