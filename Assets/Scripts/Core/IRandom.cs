using System;
using System.Collections.Generic;

namespace NightCafe.Core
{
    /// <summary>
    /// Injectable randomness so spawn decisions can be tested deterministically.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Uniform value in [0, 1).</summary>
        float NextFloat();

        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        int NextInt(int minInclusive, int maxExclusive);
    }

    public sealed class UnityRandom : IRandom
    {
        public float NextFloat() => UnityEngine.Random.value;

        public int NextInt(int minInclusive, int maxExclusive) =>
            UnityEngine.Random.Range(minInclusive, maxExclusive);
    }

    /// <summary>Repeatable randomness (1.2.0 daily challenge: the same seed, the same shift).</summary>
    public sealed class SeededRandom : IRandom
    {
        readonly System.Random _random;

        public SeededRandom(int seed) => _random = new System.Random(seed);

        public float NextFloat() => (float)_random.NextDouble();

        public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    }

    /// <summary>The services keep one of these; a shift switches it between Unity's dice and a seed.</summary>
    public sealed class ShiftRandom : IRandom
    {
        IRandom _inner = new UnityRandom();

        /// <summary>null = ordinary random shift.</summary>
        public void Reseed(int? seed) => _inner = seed.HasValue ? new SeededRandom(seed.Value) : new UnityRandom();

        public float NextFloat() => _inner.NextFloat();

        public int NextInt(int minInclusive, int maxExclusive) => _inner.NextInt(minInclusive, maxExclusive);
    }

    public static class RandomExtensions
    {
        public static T UniformPick<T>(this IRandom rng, IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Cannot pick from an empty list.", nameof(items));

            return items[rng.NextInt(0, items.Count)];
        }

        /// <summary>
        /// Picks an entry by weight. Weights need not sum to exactly 1; the roll is scaled to their total.
        /// </summary>
        public static T WeightedPick<T>(this IRandom rng, IReadOnlyList<T> items, IReadOnlyList<float> weights)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Cannot pick from an empty list.", nameof(items));
            if (weights == null || weights.Count != items.Count)
                throw new ArgumentException("Weights must match items.", nameof(weights));

            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
                total += weights[i];

            float roll = rng.NextFloat() * total;
            float cumulative = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return items[i];
            }

            return items[items.Count - 1];
        }
    }
}
