using System;
using NightCafe.Core;
using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    public sealed class DailyChallengeTests
    {
        [Test]
        public void TheDayIsTheSeed()
        {
            string key = DailyChallenge.Key(new DateTime(2026, 11, 7, 23, 59, 0, DateTimeKind.Utc));
            Assert.AreEqual("2026-11-07", key);
            Assert.AreEqual(20261107, DailyChallenge.Seed(key));
        }

        [Test]
        public void TheSameSeedRollsTheSameShift()
        {
            var a = new ShiftRandom();
            var b = new ShiftRandom();
            a.Reseed(20261107);
            b.Reseed(20261107);
            for (int i = 0; i < 50; i++)
                Assert.AreEqual(a.NextInt(0, 4), b.NextInt(0, 4));
        }

        [Test]
        public void OnlyTheFirstAttemptOfTheDayCounts()
        {
            var profile = new ProfileService(new InMemoryProfileStore());
            Assert.IsTrue(profile.TryRecordDaily("2026-11-07", 120));
            Assert.IsFalse(profile.TryRecordDaily("2026-11-07", 300), "a second go is practice");
            Assert.AreEqual(120, profile.DailyScore("2026-11-07"));
            Assert.AreEqual(0, profile.DailyScore("2026-11-08"));
            Assert.IsTrue(profile.TryRecordDaily("2026-11-08", 40));
        }

        [Test]
        public void StatsAndTheDailyTravelWithTheCloudCopy()
        {
            var phone = new ProfileService(new InMemoryProfileStore());
            phone.RecordShift(cups: 80, streak: 30, level: 5);
            phone.RecordShift(cups: 20, streak: 12, level: 2);
            phone.TryRecordDaily("2026-11-07", 150);

            var tablet = new ProfileService(new InMemoryProfileStore());
            tablet.RecordShift(cups: 10, streak: 40, level: 1);
            tablet.TryRecordDaily("2026-11-06", 90);

            Assert.IsTrue(tablet.Merge(phone.Json));
            Assert.AreEqual(2, tablet.ShiftsPlayed);
            Assert.AreEqual(100, tablet.CupsCaught);
            Assert.AreEqual(40, tablet.LongestStreak);
            Assert.AreEqual(5, tablet.HighestLevel);
            Assert.IsTrue(tablet.DailyDone("2026-11-07"), "the later day wins");
            Assert.AreEqual(150, tablet.DailyScore("2026-11-07"));
            Assert.IsFalse(tablet.Merge(phone.Json));
        }
    }
}
