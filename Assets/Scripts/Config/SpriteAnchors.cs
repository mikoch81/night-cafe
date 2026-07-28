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
            { "cup_broken", new Vector2(0.5000f, 0.5094f) },
            { "machine_head", new Vector2(0.4783f, 0.5476f) },
            { "stain", new Vector2(0.4375f, 0.3934f) }
        };

        /// <summary>Returns false for sprites that should keep a centred pivot.</summary>
        public static bool TryGet(string spriteName, out Vector2 pivot) =>
            Pivots.TryGetValue(spriteName, out pivot);
    }
}
