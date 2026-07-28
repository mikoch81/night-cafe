using System;
using System.Collections.Generic;
using NightCafe.Core;
using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    public sealed class ClockFormatterTests
    {
        [Test]
        public void FormatsHoursAndMinutesZeroPadded()
        {
            Assert.AreEqual("03:07", ClockFormatter.Format(new DateTime(2026, 1, 1, 3, 7, 0), true));
            Assert.AreEqual("00:00", ClockFormatter.Format(new DateTime(2026, 1, 1, 0, 0, 0), true));
            Assert.AreEqual("23:59", ClockFormatter.Format(new DateTime(2026, 1, 1, 23, 59, 0), true));
        }

        [Test]
        public void BlinkingTheColonKeepsTheWidthConstant()
        {
            var time = new DateTime(2026, 1, 1, 3, 7, 0);

            string on = ClockFormatter.Format(time, true);
            string off = ClockFormatter.Format(time, false);

            Assert.AreEqual("03 07", off);
            Assert.AreEqual(on.Length, off.Length, "mono digits would jitter if the width changed");
        }
    }

    public sealed class CatPathTests
    {
        [Test]
        public void WalksFromStartToEnd()
        {
            Assert.AreEqual(-6f, CatPath.XAt(0f, -6f, 6f), 0.0001f);
            Assert.AreEqual(0f, CatPath.XAt(0.5f, -6f, 6f), 0.0001f);
            Assert.AreEqual(6f, CatPath.XAt(1f, -6f, 6f), 0.0001f);
        }

        [Test]
        public void ClampsBeyondTheEnds()
        {
            Assert.AreEqual(-6f, CatPath.XAt(-1f, -6f, 6f), 0.0001f);
            Assert.AreEqual(6f, CatPath.XAt(2f, -6f, 6f), 0.0001f);
        }

        [Test]
        public void FrameAlternates()
        {
            Assert.AreEqual(0, CatPath.FrameIndexAt(0f, 6f));
            Assert.AreEqual(1, CatPath.FrameIndexAt(0.17f, 6f));
            Assert.AreEqual(0, CatPath.FrameIndexAt(0.34f, 6f));
        }

        [Test]
        public void FrameFlipsNineTimesAcrossTheCrossing()
        {
            const float duration = 1.6f;
            const float rate = 6f;

            int flips = 0;
            int previous = CatPath.FrameIndexAt(0f, rate);
            for (float t = 0f; t <= duration; t += 0.001f)
            {
                int current = CatPath.FrameIndexAt(t, rate);
                if (current != previous)
                    flips++;

                previous = current;
            }

            Assert.AreEqual(9, flips);
        }

        [Test]
        public void FadesInAndOutAtTheEdges()
        {
            Assert.AreEqual(0f, CatPath.EdgeAlpha(0f), 0.0001f);
            Assert.AreEqual(1f, CatPath.EdgeAlpha(0.5f), 0.0001f);
            Assert.AreEqual(0f, CatPath.EdgeAlpha(1f), 0.0001f);
            Assert.AreEqual(0.5f, CatPath.EdgeAlpha(0.05f), 0.0001f);
        }
    }

    public sealed class CatCueServiceTests
    {
        sealed class StubRandom : IRandom
        {
            readonly Queue<float> _values = new();

            public StubRandom(params float[] values)
            {
                foreach (float value in values)
                    _values.Enqueue(value);
            }

            public float NextFloat() => _values.Count > 0 ? _values.Dequeue() : 0f;

            public int NextInt(int minInclusive, int maxExclusive) => minInclusive;
        }

        [Test]
        public void MeowsBelowTheChanceThreshold()
        {
            var service = new CatCueService(0.30f, new StubRandom(0.299f, 0.300f, 0.301f));

            Assert.IsTrue(service.ShouldMeow());
            Assert.IsFalse(service.ShouldMeow(), "the threshold itself is exclusive");
            Assert.IsFalse(service.ShouldMeow());
        }

        [Test]
        public void ZeroChanceNeverMeows()
        {
            var service = new CatCueService(0f, new StubRandom(0f, 0.0001f));

            Assert.IsFalse(service.ShouldMeow());
            Assert.IsFalse(service.ShouldMeow());
        }
    }

    public sealed class HapticPatternsTests
    {
        [Test]
        public void GameOverIsThreePulses()
        {
            CollectionAssert.AreEqual(new long[] { 0, 80, 60, 80, 60, 80 }, HapticPatterns.Pulses(3, 80, 60));
        }

        [Test]
        public void SinglePulseHasNoGap()
        {
            CollectionAssert.AreEqual(new long[] { 0, 15 }, HapticPatterns.Pulses(1, 15, 60));
        }

        [Test]
        public void ZeroPulsesIsEmpty()
        {
            CollectionAssert.IsEmpty(HapticPatterns.Pulses(0, 80, 60));
        }
    }

    public sealed class AudioLevelsTests
    {
        [Test]
        public void MinusEighteenDecibelsMatchesTheGddOffset()
        {
            Assert.AreEqual(0.12589f, AudioLevels.LinearGain(-18f), 0.00001f);
        }

        [Test]
        public void ZeroDecibelsIsUnityGain()
        {
            Assert.AreEqual(1f, AudioLevels.LinearGain(0f), 0.00001f);
        }

        [Test]
        public void MinusSixDecibelsIsAboutHalf()
        {
            Assert.AreEqual(0.50119f, AudioLevels.LinearGain(-6f), 0.00001f);
        }
    }

    public sealed class SettingsServiceTests
    {
        [Test]
        public void EverythingDefaultsToOn()
        {
            var service = new SettingsService(new InMemorySettingsStore());

            Assert.IsTrue(service.MusicEnabled);
            Assert.IsTrue(service.SfxEnabled);
            Assert.IsTrue(service.HapticsEnabled);
        }

        [Test]
        public void EachToggleFlipsExactlyOneFlag()
        {
            var service = new SettingsService(new InMemorySettingsStore());

            service.ToggleMusic();
            Assert.IsFalse(service.MusicEnabled);
            Assert.IsTrue(service.SfxEnabled);
            Assert.IsTrue(service.HapticsEnabled);

            service.ToggleHaptics();
            Assert.IsFalse(service.MusicEnabled);
            Assert.IsTrue(service.SfxEnabled);
            Assert.IsFalse(service.HapticsEnabled);
        }

        [Test]
        public void ToggleRaisesChangedOnce()
        {
            var service = new SettingsService(new InMemorySettingsStore());
            int events = 0;
            service.Changed += () => events++;

            service.ToggleSfx();

            Assert.AreEqual(1, events);
        }

        [Test]
        public void SettingsRoundTripThroughTheStore()
        {
            var store = new InMemorySettingsStore();
            var first = new SettingsService(store);
            first.ToggleMusic();
            first.ToggleHaptics();

            var second = new SettingsService(store);

            Assert.IsFalse(second.MusicEnabled);
            Assert.IsTrue(second.SfxEnabled);
            Assert.IsFalse(second.HapticsEnabled);
        }
    }
}
