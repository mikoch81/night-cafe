using System.Collections.Generic;
using NightCafe.Core;
using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>A shell finish: a multiply tint over device_shell.png and the wood behind it.</summary>
    public readonly struct Skin
    {
        public readonly string Id;
        public readonly string Name;
        public readonly Color ShellTint;

        public Skin(string id, string name, Color shellTint)
        {
            Id = id;
            Name = name;
            ShellTint = shellTint;
        }
    }

    /// <summary>
    /// GDD 6 shell skins and their unlock rules. Walnut is the factory finish; the rest are
    /// earned: Ash at 250 in Mode A, Onyx at 500 in Mode B, Neon by rolling the counter past 999.
    /// The art is one walnut sprite, so every other finish is a multiply tint - which is why
    /// none of them can be lighter than the walnut.
    /// </summary>
    public static class SkinCatalog
    {
        public const string DefaultId = "walnut";
        public const string AshId = "ash";
        public const string OnyxId = "onyx";
        public const string NeonId = "neon";

        public const int AshScoreModeA = 250;
        public const int OnyxScoreModeB = 500;

        static readonly Skin[] Skins =
        {
            new(DefaultId, "WALNUT", Color.white),
            new(AshId, "ASH", new Color(0.78f, 0.84f, 0.74f)),
            new(OnyxId, "ONYX", new Color(0.32f, 0.33f, 0.38f)),
            new(NeonId, "NEON", new Color(0.62f, 0.34f, 0.86f))
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
        /// Skins a finished round earns. Checked on the uncapped total: a 1 050 run in Mode A
        /// still passes 250, and a rollover is a rollover whichever mode it happened in.
        /// </summary>
        public static void UnlockedBy(GameMode mode, int totalScore, bool rolledOver, List<string> result)
        {
            result.Clear();

            if (mode == GameMode.A && totalScore >= AshScoreModeA)
                result.Add(AshId);

            if (mode == GameMode.B && totalScore >= OnyxScoreModeB)
                result.Add(OnyxId);

            if (rolledOver)
                result.Add(NeonId);
        }
    }
}
