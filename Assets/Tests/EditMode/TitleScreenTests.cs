using System;
using NightCafe.Config;
using NightCafe.Core;
using NightCafe.EditorTools;
using NightCafe.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NightCafe.Tests
{
    /// <summary>The painted title and end-of-shift dressing (GDD 5.2, 6): copy, clock hands, style slots.</summary>
    public sealed class TitleScreenTests
    {
        [Test]
        public void GameOverCopySplitsIntoHeaderScoreRecordAndFooter()
        {
            GameOverCopy copy = GameOverCopy.Build(42, 317, false, null);
            Assert.AreEqual("END OF SHIFT", copy.Header);
            Assert.AreEqual("042", copy.Score);
            Assert.AreEqual("BEST 317", copy.Record);
            Assert.AreEqual("TAP TO RESTART", copy.Footer);
            Assert.IsFalse(copy.NewRecord);
        }

        [Test]
        public void GameOverCopyCelebratesARecordAndAnUnlock()
        {
            GameOverCopy copy = GameOverCopy.Build(999, 999, true, "Ash");
            Assert.AreEqual("NEW BEST!", copy.Record);
            Assert.IsTrue(copy.NewRecord);
            Assert.AreEqual("ASH UNLOCKED\nTAP TO RESTART", copy.Footer);
        }

        [Test]
        public void ClockHandsTurnClockwiseAndTheHourHandDrifts()
        {
            var threeOClock = new DateTime(2026, 9, 15, 15, 0, 0);
            Assert.AreEqual(90f, ClockWidget.HourAngle(threeOClock), 1e-3f, "15:00 reads as three on a twelve-hour dial");
            Assert.AreEqual(0f, ClockWidget.MinuteAngle(threeOClock), 1e-3f);

            var halfPastNine = new DateTime(2026, 9, 15, 21, 30, 0);
            Assert.AreEqual(285f, ClockWidget.HourAngle(halfPastNine), 1e-3f, "half past nine: the hour hand is halfway to ten");
            Assert.AreEqual(180f, ClockWidget.MinuteAngle(halfPastNine), 1e-3f);
        }

        [Test]
        public void ArtStyleDressesTheTitleOnlyWithWhatIsOnDisk()
        {
            var art = AssetDatabase.LoadAssetAtPath<ScreenStyle>("Assets/Settings/ScreenStyle_Art.asset");
            Assert.IsNotNull(art, "run NightCafe/Build Scene Setup first");

            Assert.IsTrue(art.titleBaristaWipes, "Miro wipes the counter on the painted title");
            Assert.Greater(art.gameOverDim, 0.55f, "the lights go further down than on the LCD");
            Assert.AreEqual(5, art.toggleOffsets.Length, "one pinned note per toggle");

            foreach ((string slot, string file) in NightCafeSetup.ArtTitleSprites)
            {
                bool onDisk = System.IO.File.Exists($"{NightCafeSetup.ScreenV3Dir}/{file}");
                Sprite sprite = slot switch
                {
                    "titleSign" => art.titleSign,
                    "clockFace" => art.clockFace,
                    "cardA" => art.toggleCards.Length > 0 ? art.toggleCards[0] : null,
                    "cardB" => art.toggleCards.Length > 1 ? art.toggleCards[1] : null,
                    "resultCard" => art.resultCard,
                    "catAsleepA" => art.catAsleepA,
                    _ => null,
                };
                if (slot == "catAsleepB")
                    continue; // borrows frame A when absent
                Assert.AreEqual(onDisk, sprite != null, $"{slot} must mirror {file} on disk");
            }
        }

        [Test]
        public void RetroStyleKeepsTheAuthoredTitle()
        {
            var retro = AssetDatabase.LoadAssetAtPath<ScreenStyle>("Assets/Settings/ScreenStyle_Retro.asset");
            Assert.IsNotNull(retro, "run NightCafe/Build Scene Setup first");
            Assert.IsNull(retro.titleSign);
            Assert.IsNull(retro.clockFace);
            Assert.IsNull(retro.resultCard);
            Assert.IsNull(retro.catAsleepA);
            Assert.IsFalse(retro.titleBaristaWipes, "the LCD hides Miro so the clock stays readable");
            Assert.AreEqual(0, retro.toggleOffsets.Length, "the authored row of toggles");
            Assert.AreEqual(new Vector2(0f, -0.30f), retro.titlePosition);
        }
    }
}
