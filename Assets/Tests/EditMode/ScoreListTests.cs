using System;
using System.Collections.Generic;
using NightCafe.Core;
using NightCafe.Services;
using NightCafe.UI;
using NUnit.Framework;

namespace NightCafe.Tests
{
    /// <summary>The local top 10 per mode and the four-digit counter (review 2026-09-26).</summary>
    public sealed class ScoreListTests
    {
        static readonly DateTime Day = new(2026, 9, 26);

        [Test]
        public void ShiftsLandOnTheListBestFirstAndPerMode()
        {
            var profile = new ProfileService(new InMemoryProfileStore());

            profile.SubmitScore(GameMode.A, 120, Day, out int first);
            profile.SubmitScore(GameMode.A, 300, Day, out int second);
            profile.SubmitScore(GameMode.B, 50, Day, out int other);
            profile.SubmitScore(GameMode.A, 200, Day, out int third);

            Assert.AreEqual(1, first);
            Assert.AreEqual(1, second, "a better shift goes to the top");
            Assert.AreEqual(1, other, "mode B has its own list");
            Assert.AreEqual(2, third);

            IReadOnlyList<ScoreEntry> a = profile.TopScores(GameMode.A);
            CollectionAssert.AreEqual(new[] { 300, 200, 120 }, Scores(a));
            Assert.AreEqual("2026-09-26", a[0].date);
            Assert.AreEqual(1, profile.TopScores(GameMode.B).Count);
        }

        [Test]
        public void TheListKeepsTenAndATieStaysBehindTheOlderShift()
        {
            var profile = new ProfileService(new InMemoryProfileStore());
            for (int i = 1; i <= ProfileService.TopScoreCount; i++)
                profile.SubmitScore(GameMode.A, i * 10, Day, out _);

            profile.SubmitScore(GameMode.A, 5, Day, out int tooLow);
            Assert.AreEqual(0, tooLow, "below the tenth place");

            profile.SubmitScore(GameMode.A, 50, Day, out int tie);
            Assert.AreEqual(7, tie, "100..60 ahead, then the older 50");
            Assert.AreEqual(ProfileService.TopScoreCount, profile.TopScores(GameMode.A).Count);
            Assert.AreEqual(20, Scores(profile.TopScores(GameMode.A))[9], "the old tenth place (10) fell off");
        }

        [Test]
        public void AnEmptyShiftIsNotListed()
        {
            var profile = new ProfileService(new InMemoryProfileStore());
            profile.SubmitScore(GameMode.A, 0, Day, out int rank);
            Assert.AreEqual(0, rank);
            Assert.AreEqual(0, profile.TopScores(GameMode.A).Count);
        }

        [Test]
        public void TheListSurvivesARestartAndAVersionOneProfileSeedsIt()
        {
            var store = new InMemoryProfileStore();
            new ProfileService(store).SubmitScore(GameMode.B, 777, Day, out _);
            Assert.AreEqual(777, new ProfileService(store).TopScores(GameMode.B)[0].score);

            // 1.0.0 wrote schema 1 with records only.
            var old = new ProfileService(new InMemoryProfileStore(
                "{\"schemaVersion\":1,\"bestScores\":[415,0],\"unlockedSkins\":[],\"selectedSkin\":\"\",\"selectedMode\":0}"));
            Assert.AreEqual(415, old.Best(GameMode.A));
            CollectionAssert.AreEqual(new[] { 415 }, Scores(old.TopScores(GameMode.A)), "the old record opens the list");
            Assert.AreEqual(0, old.TopScores(GameMode.B).Count);
        }

        [Test]
        public void TheReceiptNamesThePlaceOnTheList()
        {
            Assert.AreEqual("#3 ON LIST · BEST 1050", GameOverCopy.Build(640, 1050, false, null, 3).Record);
            Assert.AreEqual("NEW BEST!", GameOverCopy.Build(1200, 1200, true, null, 1).Record);
            Assert.AreEqual("BEST 1050", GameOverCopy.Build(12, 1050, false, null, 0).Record);
            Assert.AreEqual("1050", GameOverCopy.Build(1050, 1050, true, null, 1).Score, "four digits, no wrap");
        }

        [Test]
        public void TheMenuListsScoresWithTheirDates()
        {
            Assert.AreEqual("NO SHIFTS YET", PauseMenuView.FormatScores(new List<ScoreEntry>()));

            var list = new List<ScoreEntry>
            {
                new(GameMode.A, 1050, Day),
                new() { mode = 0, score = 42, date = "" },
            };
            Assert.AreEqual(" 1.  1050   2026-09-26\n 2.  042", PauseMenuView.FormatScores(list));
        }

        static int[] Scores(IReadOnlyList<ScoreEntry> entries)
        {
            var result = new int[entries.Count];
            for (int i = 0; i < entries.Count; i++)
                result[i] = entries[i].score;
            return result;
        }
    }
}
