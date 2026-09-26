using System.IO;
using NightCafe.Config;
using NightCafe.EditorTools;
using NightCafe.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NightCafe.Tests
{
    /// <summary>GDD 5.2: one painted screen style, honest about what it has; RETRO is gone.</summary>
    public sealed class ScreenStyleTests
    {
        const string ArtPath = "Assets/Settings/ScreenStyle_Art.asset";
        const string RetroPath = "Assets/Settings/ScreenStyle_Retro.asset";

        [Test]
        public void RetroStyleIsGone()
        {
            Assert.IsFalse(File.Exists(RetroPath), "the segmented RETRO style was dropped after the closed test");
        }

        [Test]
        public void ArtStyleIsCompleteExactlyWhenEveryPaintedFileExists()
        {
            var art = AssetDatabase.LoadAssetAtPath<ScreenStyle>(ArtPath);
            Assert.IsNotNull(art, "run NightCafe/Build Scene Setup first");

            bool allFiles = true;
            foreach ((string _, string file) in NightCafeSetup.ArtSprites)
                allFiles &= File.Exists($"{NightCafeSetup.ScreenV3Dir}/{file}");
            Assert.AreEqual(allFiles, art.complete, "complete must mirror the files on disk");
            if (art.complete)
                Assert.IsTrue(art.HasPaintedCups && art.cupsByOrder.Length == 4, "one painted cup per order colour");
        }

        [Test]
        public void TheStyleBringsTheRecordedSoundSet()
        {
            var art = AssetDatabase.LoadAssetAtPath<ScreenStyle>(ArtPath);
            Assert.IsNotNull(art);

            Assert.IsNotNull(art.sounds, "ART has the recorded set");
            Assert.AreNotEqual(AssetDatabase.GetAssetPath(art.sounds), "Assets/Settings/AudioConfig.asset");

            foreach (GameSfx sfx in System.Enum.GetValues(typeof(GameSfx)))
                Assert.IsNotNull(art.sounds.Clip(sfx), $"ART clip missing for {sfx} - run tools/prep_audio.py");
            Assert.IsNotNull(art.sounds.lofiLoop, "ART lo-fi loop");
            Assert.IsNotNull(art.sounds.ambience, "ART room tone");
            Assert.That(art.sounds.lofiLoop.length, Is.InRange(60f, 90f), "GDD 5.3: a 60-90 s loop");
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
