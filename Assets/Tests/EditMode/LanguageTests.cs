using NightCafe.Core;
using NightCafe.Services;
using NUnit.Framework;
using UnityEngine;

namespace NightCafe.Tests
{
    /// <summary>The English and Polish copy (1.1.0, review 2026-09-26).</summary>
    public sealed class LanguageTests
    {
        [TearDown]
        public void BackToEnglish() => Loc.Set(Language.English);

        [Test]
        public void EveryLineExistsInBothLanguages()
        {
            Assert.IsTrue(Loc.Complete(out Txt missing), missing.ToString());
            Assert.IsTrue(BaristaQuips.Complete(out Quip quip), quip.ToString());
        }

        [Test]
        public void APolishPhoneStartsInPolishAndTheChoiceIsKept()
        {
            Assert.AreEqual(Language.Polish, Loc.FromSystem(SystemLanguage.Polish));
            Assert.AreEqual(Language.English, Loc.FromSystem(SystemLanguage.German));

            var store = new InMemorySettingsStore();
            var settings = new SettingsService(store, Language.Polish);
            Assert.AreEqual(Language.Polish, settings.Language, "first run: the phone's language");
            settings.ToggleLanguage();
            Assert.AreEqual(Language.English, new SettingsService(store, Language.Polish).Language,
                "a choice made in the menu outlives the phone's setting");
        }

        [Test]
        public void TheReceiptAndTheMenuSpeakPolish()
        {
            Loc.Set(Language.Polish);
            GameOverCopy copy = GameOverCopy.Build(120, 300, false, "JESION", 3);
            Assert.AreEqual("KONIEC ZMIANY", copy.Header);
            StringAssert.Contains("NA LIŚCIE", copy.Record);
            StringAssert.Contains("JESION ODBLOKOWANY", copy.Footer);
            Assert.AreEqual("WŁ.", Loc.OnOff(true));
            Assert.AreEqual("ORZECH", Loc.Skin(0, "WALNUT"));
            Assert.AreEqual("BEZ KOFEINY", Loc.Order(3, "DECAF"));
            Assert.AreEqual("JESZCZE ŻADNEJ ZMIANY", UI.PauseMenuView.FormatScores(null));
        }

        [Test]
        public void TheChalkLetteringStaysPlainAscii()
        {
            // BEST and LV are set in Cabin Sketch, which has no Polish diacritics.
            foreach (Language language in new[] { Language.English, Language.Polish })
            {
                Loc.Set(language);
                foreach (char c in Loc.F(Txt.HudBest, 7) + Loc.F(Txt.HudLevel, 7))
                    Assert.Less(c, 128, $"{language}: '{c}'");
            }
        }

        [Test]
        public void MiroSpeaksTheCurrentLanguage()
        {
            var quips = new BaristaQuips(new UnityRandom());
            Loc.Set(Language.Polish);
            StringAssert.Contains("Paprika na moim stołku", quips.Line(Quip.CatOnLadder));
            Loc.Set(Language.English);
            StringAssert.Contains("Paprika's on my stool", quips.Line(Quip.CatOnLadder));
        }
    }
}
