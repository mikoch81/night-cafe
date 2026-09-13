using System.Collections.Generic;
using UnityEngine;

namespace NightCafe.Config
{
    /// <summary>
    /// Normalised pivots per sprite file. The art is drawn on padded canvases, so a centred
    /// pivot puts every sprite off its mark - see the alpha bounds in the M2 plan.
    ///
    /// This is runtime (not editor) code on purpose: it keeps the two invariants below
    /// unit-testable, and getting them wrong is invisible in a still screenshot.
    /// </summary>
    public static class SpriteAnchors
    {
        /// <summary>
        /// Feet-and-body anchor shared by ALL four barista poses. It must be shared:
        /// barista_miss is only 183 px wide against barista_catch's 340, so per-sprite
        /// content centres would make the character jump sideways whenever the pose swaps.
        /// </summary>
        public static readonly Vector2 Barista = new(0.4352f, 0.1100f);

        /// <summary>
        /// Shared body anchor for both cat frames - the mop swings between frames
        /// (cat_a is 344 px wide, cat_b 400), the cat itself must not.
        /// </summary>
        public static readonly Vector2 Cat = new(0.3400f, 0.0446f);

        static readonly Dictionary<string, Vector2> Pivots = new()
        {
            { "barista_up", Barista },
            { "barista_down", Barista },
            { "barista_catch", Barista },
            { "barista_miss", Barista },
            { "cat_a", Cat },
            { "cat_b", Cat },
            { "cup", new Vector2(0.5000f, 0.6029f) },
            { "cup_broken", new Vector2(0.5000f, 0.5000f) },
            { "machine_head", new Vector2(0.4783f, 0.5476f) },
            { "stain", new Vector2(0.4375f, 0.3934f) }
        };

        /// <summary>
        /// The painted set (Assets/Art/screen_v3, GDD 5.2): every Miro pose is a differently
        /// trimmed cut-out, so each carries its own feet anchor (x = centre of the feet, measured
        /// from the alpha; y just above the trimmed bottom). The cat anchors sit on the paws and
        /// the body, not the mop; the machine's anchor is its spout, which the rail starts at.
        /// </summary>
        static readonly Dictionary<string, Vector2> PaintedPivots = new()
        {
            { "miro_up", new Vector2(0.3123f, 0.0100f) },
            { "miro_down", new Vector2(0.2535f, 0.0100f) }, // the sheet drew this pose facing left; cut with --flip down
            { "miro_catch", new Vector2(0.3431f, 0.0100f) },
            { "miro_miss", new Vector2(0.4509f, 0.0100f) },
            { "miro_wipe", new Vector2(0.3810f, 0.0100f) },
            { "sable_a", new Vector2(0.4300f, 0.0700f) },
            { "sable_b", new Vector2(0.4500f, 0.0700f) },
            { "machine_head", new Vector2(0.6200f, 0.3200f) },
            { "cup_espresso", new Vector2(0.5000f, 0.0300f) },
            { "cup_caramel", new Vector2(0.5000f, 0.0300f) },
            { "cup_latte", new Vector2(0.5000f, 0.0300f) },
            { "cup_decaf", new Vector2(0.5000f, 0.0300f) },
            { "cup_broken", new Vector2(0.5000f, 0.5000f) },
            { "stain", new Vector2(0.4375f, 0.3934f) },
            { "step", new Vector2(0.5000f, 0.9550f) }
        };

        /// <summary>Returns false for sprites that should keep a centred pivot.</summary>
        public static bool TryGet(string spriteName, out Vector2 pivot) =>
            Pivots.TryGetValue(spriteName, out pivot);

        public static bool TryGet(string spriteName, bool painted, out Vector2 pivot) =>
            painted ? PaintedPivots.TryGetValue(spriteName, out pivot) : Pivots.TryGetValue(spriteName, out pivot);
    }
}
