using System.IO;
using NightCafe.Config;
using NightCafe.EditorTools;
using NightCafe.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NightCafe.Tests
{
    /// <summary>GDD 5.2 / 5.2a: two screen styles, RETRO always whole, ART honest about what it has.</summary>
    public sealed class ScreenStyleTests
    {
        const string ArtPath = "Assets/Settings/ScreenStyle_Art.asset";
        const string RetroPath = "Assets/Settings/ScreenStyle_Retro.asset";

        [Test]
        public void RetroStyleIsTheCompleteSegmentedLook()
        {
            var retro = AssetDatabase.LoadAssetAtPath<ScreenStyle>(RetroPath);
            Assert.IsNotNull(retro, "run NightCafe/Build Scene Setup first");
            Assert.IsTrue(retro.complete);
            Assert.IsTrue(retro.monochrome);
            Assert.IsTrue(retro.bloom, "GDD 5.2a keeps the bloom on the LCD look");
            Assert.IsTrue(retro.ghostsAllowed);
            Assert.IsFalse(retro.HasPaintedCups, "RETRO tints one white cup mask");
            foreach (Sprite sprite in new[] { retro.background, retro.machineHead, retro.baristaUp, retro.baristaDown,
                         retro.baristaCatch, retro.baristaMiss, retro.baristaWipe, retro.catA, retro.catB, retro.cup,
                         retro.cupBroken, retro.stain, retro.orderPanel })
                Assert.IsNotNull(sprite, "a RETRO slot is empty");
            Assert.IsNotNull(retro.digitFont, "DSEG7 digits");
            Assert.IsNotNull(retro.letterFont, "DSEG14 letters");
        }

        [Test]
        public void ArtStyleIsCompleteExactlyWhenEveryPaintedFileExists()
        {
            var art = AssetDatabase.LoadAssetAtPath<ScreenStyle>(ArtPath);
            Assert.IsNotNull(art, "run NightCafe/Build Scene Setup first");
            Assert.IsFalse(art.monochrome);
            Assert.IsFalse(art.bloom, "GDD 5.2: no bloom on the painted diorama");
            Assert.IsFalse(art.ghostsAllowed);

            bool allFiles = true;
            foreach ((string _, string file) in NightCafeSetup.ArtSprites)
                allFiles &= File.Exists($"{NightCafeSetup.ScreenV3Dir}/{file}");
            Assert.AreEqual(allFiles, art.complete, "complete must mirror the files on disk");
            if (art.complete)
                Assert.IsTrue(art.HasPaintedCups && art.cupsByOrder.Length == 4, "one painted cup per order colour");
        }

        [Test]
        public void RetroScreenSettingPersistsAndDefaultsToThePaintedLook()
        {
            var store = new InMemorySettingsStore();
            var settings = new NightCafe.Services.SettingsService(store);
            Assert.IsFalse(settings.RetroScreen);

            int changes = 0;
            settings.Changed += () => changes++;
            settings.ToggleRetroScreen();
            Assert.IsTrue(settings.RetroScreen);
            Assert.AreEqual(1, changes);
            Assert.IsTrue(new NightCafe.Services.SettingsService(store).RetroScreen, "the choice survives a restart");
        }

        [Test]
        public void CupSkinFallsBackToTintingWithoutPaintedCups()
        {
            var style = ScriptableObject.CreateInstance<ScreenStyle>();
            try
            {
                Assert.IsFalse(style.HasPaintedCups);
                Assert.IsNull(style.CupFor(2));
                var painted = new Sprite[4];
                painted[0] = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
                style.cupsByOrder = painted;
                Assert.IsTrue(style.HasPaintedCups);
                Assert.AreSame(painted[0], style.CupFor(0));
                Assert.AreSame(style.cup, style.CupFor(3), "a missing colour falls back to the base cup");
            }
            finally
            {
                Object.DestroyImmediate(style);
            }
        }
    }
}
