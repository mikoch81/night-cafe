using System.Collections.Generic;
using NightCafe.Core;
using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>
    /// A shell finish. Each skin has its own rendered sprite (device_shell_&lt;id&gt;.png from
    /// tools/shell_render.py); ShellTint is the multiply fallback when that sprite is missing,
    /// Background the camera clear colour that continues the finish past the sprite edge.
    /// </summary>
    public readonly struct Skin
    {
        public readonly string Id;
        public readonly string Name;
        public readonly Color ShellTint;
        public readonly Color Background;

        public Skin(string id, string name, Color shellTint, Color background)
        {
            Id = id;
            Name = name;
            ShellTint = shellTint;
            Background = background;
        }
    }

    /// <summary>
    /// GDD 6 shell skins and their unlock rules. Walnut is the factory finish; the rest are
    /// earned: Ash at 250 in Mode A, Onyx at 500 in Mode B, Neon by rolling the counter past 999.
    /// </summary>
    public static class SkinCatalog
    {
        public const string DefaultId = "walnut";
        public const string AshId = "ash";
        public const string OnyxId = "onyx";
        public const string NeonId = "neon";

        static readonly Skin[] Skins =
        {
            new(DefaultId, "WALNUT", Color.white, new Color(0.329f, 0.188f, 0.102f)),
            new(AshId, "ASH", new Color(0.78f, 0.84f, 0.74f), new Color(0.62f, 0.52f, 0.38f)),
            new(OnyxId, "ONYX", new Color(0.32f, 0.33f, 0.38f), new Color(0.11f, 0.10f, 0.11f)),
            new(NeonId, "NEON", new Color(0.62f, 0.34f, 0.86f), new Color(0.13f, 0.09f, 0.19f))
        };

        public static IReadOnlyList<Skin> All => Skins;

        public static bool IsDefault(string id) => id == DefaultId;

        public static bool TryGet(string id, out Skin skin)
        {
            int index = IndexOf(id);
            skin = index >= 0 ? Skins[index] : Skins[0];
            return index >= 0;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < Skins.Length; i++)
            {
                if (Skins[i].Id == id)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Skins a finished round earns. The mode's own unlock (id + score threshold) comes from
        /// its ModeConfig; the rollover skin is a rule, not a number, so it lives here. Checked on
        /// the uncapped total: a 1 050 run still passes 250.
        /// </summary>
        public static void UnlockedBy(string modeSkinId, int modeSkinScore, int totalScore, bool rolledOver,
            List<string> result)
        {
            result.Clear();

            if (!string.IsNullOrEmpty(modeSkinId) && modeSkinScore > 0 && totalScore >= modeSkinScore)
                result.Add(modeSkinId);

            if (rolledOver)
                result.Add(NeonId);
        }
    }
}
