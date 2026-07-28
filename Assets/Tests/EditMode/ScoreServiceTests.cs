using NightCafe.Config;
using NightCafe.Services;
using NUnit.Framework;
using UnityEngine;

namespace NightCafe.Tests
{
    public sealed class ScoreServiceTests
    {
        static ScoreSettings DefaultSettings() =>
            new(pointsPerCatch: 1, comboBonusEvery: 25, comboBonusPoints: 5,
                rolloverModulo: 1000, mercyThresholds: new[] { 200, 500 }, breatherEvery: 100);

        static void Catch(ScoreService service, int times)
        {
            for (int i = 0; i < times; i++)
                service.RegisterCatch();
        }

        /// <summary>
        /// Scores exactly <paramref name="points"/> without ever completing a 25-catch series,
        /// so the running total stays predictable when a test cares about thresholds only.
        /// </summary>
        static void ScoreWithoutBonus(ScoreService service, int points)
        {
            const int chunk = 24;
            service.RegisterMiss(); // start from a clean series so no chunk completes a combo

            while (points > 0)
            {
                int step = Mathf.Min(chunk, points);
                Catch(service, step);
                service.RegisterMiss();
                points -= step;
            }
        }

        [Test]
        public void EachCatchScoresOnePoint()
        {
            var service = new ScoreService(DefaultSettings());

            Catch(service, 10);

            Assert.AreEqual(10, service.TotalScore);
            Assert.AreEqual(10, service.Combo);
        }

        [Test]
        public void ComboBonusAwardedEveryTwentyFive()
        {
            var service = new ScoreService(DefaultSettings());
            int bonuses = 0;
            service.ComboBonusAwarded += () => bonuses++;

            Catch(service, 24);
            Assert.AreEqual(0, bonuses);
            Assert.AreEqual(24, service.TotalScore);

            service.RegisterCatch();
            Assert.AreEqual(1, bonuses);
            Assert.AreEqual(30, service.TotalScore, "25 catches + 5 bonus");

            Catch(service, 25);
            Assert.AreEqual(2, bonuses);
            Assert.AreEqual(60, service.TotalScore, "50 catches + 10 bonus");
        }

        [Test]
        public void MissResetsComboButKeepsScore()
        {
            var service = new ScoreService(DefaultSettings());
            int bonuses = 0;
            service.ComboBonusAwarded += () => bonuses++;

            Catch(service, 24);
            service.RegisterMiss();
            Assert.AreEqual(0, service.Combo);
            Assert.AreEqual(24, service.TotalScore);

            service.RegisterCatch();
            Assert.AreEqual(0, bonuses, "a broken series restarts the count from one");
            Assert.AreEqual(1, service.Combo);
        }

        [Test]
        public void BreatherFiresOnEveryHundredCrossing()
        {
            var service = new ScoreService(DefaultSettings());
            int breathers = 0;
            service.BreatherTriggered += () => breathers++;

            ScoreWithoutBonus(service, 99);
            Assert.AreEqual(99, service.TotalScore);
            Assert.AreEqual(0, breathers);

            service.RegisterCatch();
            Assert.AreEqual(100, service.TotalScore);
            Assert.AreEqual(1, breathers, "score reached 100");

            ScoreWithoutBonus(service, 100);
            Assert.AreEqual(200, service.TotalScore);
            Assert.AreEqual(2, breathers, "score reached 200");
        }

        [Test]
        public void ThresholdCrossedByComboBonusFiresExactlyOnce()
        {
            var service = new ScoreService(DefaultSettings());

            // Reach 199 with a 24-catch series pending, so the next catch completes a combo of 25.
            ScoreWithoutBonus(service, 175);
            Catch(service, 24);
            Assert.AreEqual(199, service.TotalScore);
            Assert.AreEqual(24, service.Combo);

            int mercies = 0;
            int breathers = 0;
            service.MercyTriggered += () => mercies++;
            service.BreatherTriggered += () => breathers++;

            // That catch scores 1 + a 5 point bonus, jumping 199 -> 205 straight over the 200 line.
            service.RegisterCatch();

            Assert.AreEqual(205, service.TotalScore);
            Assert.AreEqual(1, mercies, "mercy fires once even though the score never equalled 200");
            Assert.AreEqual(1, breathers, "breather fires once for the same crossing");
        }

        [Test]
        public void MercyFiresAtBothThresholds()
        {
            var service = new ScoreService(DefaultSettings());
            int mercies = 0;
            service.MercyTriggered += () => mercies++;

            while (service.TotalScore < 200)
                service.RegisterCatch();
            Assert.AreEqual(1, mercies);

            while (service.TotalScore < 500)
                service.RegisterCatch();
            Assert.AreEqual(2, mercies);
        }

        [Test]
        public void RolloverWrapsDisplayAndReArmsMercy()
        {
            var service = new ScoreService(DefaultSettings());
            int rollovers = 0;
            int mercies = 0;
            service.RolloverOccurred += () => rollovers++;
            service.MercyTriggered += () => mercies++;

            while (service.TotalScore < 999)
                service.RegisterCatch();

            Assert.AreEqual(999, service.DisplayScore);
            Assert.AreEqual(0, rollovers);
            Assert.AreEqual(2, mercies, "200 and 500 of the first cycle");

            while (service.TotalScore < 1000)
                service.RegisterCatch();

            Assert.AreEqual(1, rollovers);
            Assert.AreEqual(0, service.DisplayScore, "counter wraps to 0");
            Assert.AreEqual(1000, service.TotalScore, "running total keeps climbing");

            while (service.TotalScore < 1200)
                service.RegisterCatch();

            Assert.AreEqual(3, mercies, "the 200 threshold re-arms after a rollover");
        }

        [Test]
        public void ScoreChangedReportsDisplayScore()
        {
            var service = new ScoreService(DefaultSettings());
            int last = -1;
            service.ScoreChanged += value => last = value;

            Catch(service, 3);

            Assert.AreEqual(3, last);
        }

        [Test]
        public void ResetClearsScoreAndCombo()
        {
            var service = new ScoreService(DefaultSettings());
            Catch(service, 30);

            service.Reset();

            Assert.AreEqual(0, service.TotalScore);
            Assert.AreEqual(0, service.DisplayScore);
            Assert.AreEqual(0, service.Combo);
        }
    }
}
