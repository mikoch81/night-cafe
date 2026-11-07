using System.Collections.Generic;
using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    public sealed class AwardTests
    {
        [Test]
        public void ConsoleResourceNamesMapToAwards()
        {
            Assert.IsTrue(Awards.TryMatch("achievement_wynik_1000", out Award a));
            Assert.AreEqual(Award.Score1000, a);
            Assert.IsTrue(Awards.TryMatch("achievement_straszna_dziesi_tka", out a));
            Assert.AreEqual(Award.TerribleTen, a);
            Assert.IsFalse(Awards.TryMatch("leaderboard_night_caf__tryb_a", out _));
            Assert.IsFalse(Awards.TryMatch("achievement_something_else", out _));
        }

        [Test]
        public void EveryAwardHasAName()
        {
            var seen = new HashSet<Award>();
            foreach (string name in new[] { "pierwsza_zmiana", "wynik_100", "wynik_250", "wynik_500", "wynik_1000",
                         "seria_25", "seria_50", "straszna_dziesiatka", "poziom_5", "zamowienia_50", "pogromca_papriki" })
            {
                Assert.IsTrue(Awards.TryMatch("achievement_" + name, out Award award), name);
                seen.Add(award);
            }

            Assert.AreEqual((int)Award.Count, seen.Count);

            seen.Clear();
            foreach (string name in new[] { "first_shift", "score_100", "score_250", "score_500", "score_1000",
                         "streak_25", "streak_50", "terrible_ten", "level_5", "orders_50", "paprika_tamer" })
            {
                Assert.IsTrue(Awards.TryMatch("achievement_" + name, out Award award), name);
                seen.Add(award);
            }

            Assert.AreEqual((int)Award.Count, seen.Count, "the English names (the console's default language)");
        }

        [Test]
        public void ScoreAwardsFollowTheTotal()
        {
            var list = new List<Award>();
            Awards.ForScore(260, false, list);
            CollectionAssert.AreEqual(new[] { Award.Score100, Award.Score250 }, list);
            Awards.ForScore(60, true, list);
            CollectionAssert.AreEqual(new[] { Award.ModeB50 }, list);
        }
    }
}
