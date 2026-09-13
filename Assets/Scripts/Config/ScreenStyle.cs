using TMPro;
using UnityEngine;

namespace NightCafe.Config
{
    /// <summary>
    /// Everything the LCD scene looks like, as one swappable asset (GDD 5.2 / 5.2a): the
    /// painted "ART" diorama and the segmented "RETRO" Neo-LCD. Geometry, timing and rules
    /// live elsewhere; a style only says which sprite, colour and font goes where.
    /// Built by the scene setup, never edited by hand.
    /// </summary>
    [CreateAssetMenu(menuName = "NightCafe/Screen Style", fileName = "ScreenStyle")]
    public sealed class ScreenStyle : ScriptableObject
    {
        public string id = "art";
        public string label = "ART";

        [Tooltip("Set by the setup when every sprite the style needs exists. An incomplete style is never shown; the loop falls back to RETRO.")]
        public bool complete;

        [Header("Look")]
        [Tooltip("RETRO: sprites are single-colour masks tinted amber, unlit slots dim amber. ART: full-colour sprites, tint white, unlit slots hidden.")]
        public bool monochrome = false;
        public bool bloom = false;
        [Tooltip("Segment ghosts are an LCD affectation: only a monochrome style may show them.")]
        public bool ghostsAllowed = false;

        [Header("Scene")]
        public Sprite background;
        [Tooltip("ART only: a straight counter plank laid along each rail segment (start-bend, bend-end).")]
        public Sprite plank;
        [Tooltip("Plank thickness across the rail, in LCD units.")]
        public float plankHeight = 0.5f;
        [Tooltip("Shift of the plank from the rail line, so the cups rest on its top face rather than through its middle.")]
        public Vector2 plankOffset;
        public Sprite machineHead;
        [Tooltip("ART only: the footstool drawn under the upper barista slots, so Miro is not standing in the air.")]
        public Sprite step;
        [Tooltip("Added to the LaneConfig barista slots: the painted Miro's tray sits at a different height above his feet than the LCD one's.")]
        public Vector2 baristaOffset;

        [Header("Actors")]
        public Sprite baristaUp;
        public Sprite baristaDown;
        public Sprite baristaCatch;
        public Sprite baristaMiss;
        public Sprite baristaWipe;
        public Sprite catA;
        public Sprite catB;

        [Tooltip("One cup for every style: RETRO tints it (white sprite x order colour, GDD 3).")]
        public Sprite cup;
        [Tooltip("ART: a painted cup per order colour, indexed like ModeConfig.orderColors; empty = tint `cup`.")]
        public Sprite[] cupsByOrder = new Sprite[0];
        public Sprite cupBroken;
        public Sprite stain;
        public Sprite orderPanel;
        [Tooltip("Scale of the cup drawn inside the order panel, relative to the panel (ART); RETRO paints a swatch instead.")]
        public Vector2 orderCupScale = new(0.55f, 0.55f);

        [Header("Type (null = keep the scene's font)")]
        public TMP_FontAsset digitFont;
        public TMP_FontAsset letterFont;
        public TMP_FontAsset textFont;

        [Header("Size multipliers on LaneConfig scales (1 = as authored for RETRO)")]
        public float baristaScale = 1f;
        public float cupScale = 1f;
        public float catScale = 1f;
        public float stainScale = 1f;
        public float machineHeadScale = 1f;
        public float brokenCupScale = 1f;

        public Sprite CupFor(int colour)
        {
            if (cupsByOrder != null && colour >= 0 && colour < cupsByOrder.Length && cupsByOrder[colour] != null)
                return cupsByOrder[colour];
            return cup;
        }

        /// <summary>True when the style paints cups per colour rather than tinting one mask.</summary>
        public bool HasPaintedCups => cupsByOrder != null && cupsByOrder.Length > 0 && cupsByOrder[0] != null;
    }
}
