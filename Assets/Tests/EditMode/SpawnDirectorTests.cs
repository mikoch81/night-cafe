using System.Collections.Generic;
using NightCafe.Config;
using NightCafe.Core;
using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    public sealed class SpawnDirectorTests
    {
        const float Tolerance = 0.0001f;

        sealed class FakeRandom : IRandom
        {
            readonly Queue<float> _floats = new();
            readonly Queue<int> _ints = new();

            public FakeRandom QueueFloat(params float[] values)
            {
                foreach (float value in values)
                    _floats.Enqueue(value);

                return this;
            }

            public FakeRandom QueueInt(params int[] values)
            {
                foreach (int value in values)
                    _ints.Enqueue(value);

                return this;
            }

            public float NextFloat() => _floats.Count > 0 ? _floats.Dequeue() : 0f;

            public int NextInt(int minInclusive, int maxExclusive) =>
                _ints.Count > 0 ? _ints.Dequeue() : minInclusive;
        }

        sealed class FakeOccupancy : ILaneOccupancy
        {
            readonly int[] _cups = new int[LanePositionExtensions.Count];
            readonly int[] _newestStep = new int[LanePositionExtensions.Count];

            public FakeOccupancy()
            {
                for (int i = 0; i < _newestStep.Length; i++)
                    _newestStep[i] = int.MaxValue;
            }

            public FakeOccupancy WithLane(int lane, int cups, int newestStep)
            {
                _cups[lane] = cups;
                _newestStep[lane] = newestStep;
                return this;
            }

            public int TotalActiveCups
            {
                get
                {
                    int total = 0;
                    foreach (int count in _cups)
                        total += count;

                    return total;
                }
            }

            public int CupsOnLane(int laneIndex) => _cups[laneIndex];

            public int NewestCupStep(int laneIndex) => _newestStep[laneIndex];
        }

        static SpawnSettings DefaultSettings() =>
            new(firstSpawnDelay: 1.2f,
                multipliers: new[] { 2, 3, 4 },
                weights: new[] { 0.20f, 0.50f, 0.30f },
                maxCupsPerLane: 2,
                minStepGap: 2,
                screenLimitByTempo: new[] { 2, 2, 2, 3, 3, 3, 4, 4, 4, 4 });

        [Test]
        public void FirstSpawnDelayComesFromConfig()
        {
            var director = new SpawnDirector(DefaultSettings(), new FakeRandom());

            Assert.AreEqual(1.2f, director.FirstSpawnDelay, Tolerance);
        }

        [TestCase(0.00f, 2)]
        [TestCase(0.199f, 2)]
        [TestCase(0.20f, 3)]
        [TestCase(0.699f, 3)]
        [TestCase(0.70f, 4)]
        [TestCase(0.999f, 4)]
        public void IntervalMultiplierFollowsWeights(float roll, int expectedMultiplier)
        {
            var rng = new FakeRandom().QueueFloat(roll);
            var director = new SpawnDirector(DefaultSettings(), rng);

            float interval = director.RollNextInterval(0.5f);

            Assert.AreEqual(0.5f * expectedMultiplier, interval, Tolerance);
        }

        [Test]
        public void IntervalScalesWithStepTime()
        {
            var rng = new FakeRandom().QueueFloat(0.5f);
            var director = new SpawnDirector(DefaultSettings(), rng);

            Assert.AreEqual(0.405f * 3, director.RollNextInterval(0.405f), Tolerance);
        }

        [Test]
        public void FullLaneIsNotEligible()
        {
            // T9 allows four cups on screen, so only the per-lane cap is in play here.
            var occupancy = new FakeOccupancy().WithLane(0, cups: 2, newestStep: 4);
            var rng = new FakeRandom().QueueInt(0);
            var director = new SpawnDirector(DefaultSettings(), rng);

            Assert.IsTrue(director.TryPickLane(occupancy, 9, out int lane));
            Assert.AreNotEqual(0, lane, "a lane already holding two cups must be skipped");
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, true)]
        public void MinStepGapGatesTheLane(int newestStep, bool expectedEligible)
        {
            var occupancy = new FakeOccupancy().WithLane(0, cups: 1, newestStep: newestStep);
            // The fake picks index 0 of the eligible set: lane 0 when it qualifies, lane 1 otherwise.
            var rng = new FakeRandom().QueueInt(0);
            var director = new SpawnDirector(DefaultSettings(), rng);

            Assert.IsTrue(director.TryPickLane(occupancy, 9, out int lane));

            if (expectedEligible)
                Assert.AreEqual(0, lane, "a cup two steps along leaves room behind it");
            else
                Assert.AreEqual(1, lane, "lane 0 stays gated until its newest cup has moved two steps");
        }

        [TestCase(0, 2)]
        [TestCase(3, 3)]
        [TestCase(6, 4)]
        [TestCase(9, 4)]
        public void ScreenLimitBlocksSpawnAtTempoLevel(int tempoLevel, int limit)
        {
            var occupancy = new FakeOccupancy();
            // Spread `limit` cups across lanes without filling any lane past its cap.
            for (int i = 0; i < limit; i++)
                occupancy.WithLane(i % 4, occupancy.CupsOnLane(i % 4) + 1, 4);

            var director = new SpawnDirector(DefaultSettings(), new FakeRandom().QueueInt(0));

            Assert.IsFalse(director.TryPickLane(occupancy, tempoLevel, out _),
                $"T{tempoLevel} allows at most {limit} cups on screen");
        }

        [Test]
        public void SpawnResumesWhenScreenFreesUp()
        {
            var occupancy = new FakeOccupancy().WithLane(0, 1, 4).WithLane(1, 1, 4);
            var director = new SpawnDirector(DefaultSettings(), new FakeRandom().QueueInt(0));

            Assert.IsFalse(director.TryPickLane(occupancy, 0, out _), "T0 limit of 2 reached");

            occupancy.WithLane(1, 0, int.MaxValue);

            Assert.IsTrue(director.TryPickLane(occupancy, 0, out _), "a freed slot lets the deferred spawn through");
        }

        [Test]
        public void AllLanesBlockedReturnsFalse()
        {
            var occupancy = new FakeOccupancy()
                .WithLane(0, 2, 4).WithLane(1, 2, 4).WithLane(2, 2, 4).WithLane(3, 2, 4);
            var director = new SpawnDirector(DefaultSettings(), new FakeRandom());

            Assert.IsFalse(director.TryPickLane(occupancy, 9, out _));
        }

        [Test]
        public void LaneIsPickedUniformlyFromEligibleSet()
        {
            var occupancy = new FakeOccupancy().WithLane(0, 2, 4);
            // Eligible lanes are 1, 2, 3 - index 2 of that set is lane 3.
            var rng = new FakeRandom().QueueInt(2);
            var director = new SpawnDirector(DefaultSettings(), rng);

            Assert.IsTrue(director.TryPickLane(occupancy, 9, out int lane));
            Assert.AreEqual(3, lane);
        }

        [Test]
        public void EmptyLaneIsAlwaysEligible()
        {
            var occupancy = new FakeOccupancy();
            var director = new SpawnDirector(DefaultSettings(), new FakeRandom().QueueInt(0));

            Assert.IsTrue(director.TryPickLane(occupancy, 0, out int lane));
            Assert.AreEqual(0, lane);
        }
    }
}
