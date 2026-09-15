using NightCafe.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace NightCafe.Tests
{
    /// <summary>M5: what the Play upload is built from (NightCafe/Build Scene Setup writes it).</summary>
    public sealed class ReleaseSettingsTests
    {
        [Test]
        public void LauncherIconsComeFromTheGeneratedSet()
        {
            var fg = AssetDatabase.LoadAssetAtPath<Texture2D>(NightCafeSetup.IconDir + "/icon_fg.png");
            var legacy = AssetDatabase.LoadAssetAtPath<Texture2D>(NightCafeSetup.IconDir + "/icon_legacy.png");
            Assert.IsNotNull(fg, "run tools/gen_icon.py");
            Assert.IsNotNull(legacy);
            Assert.AreEqual(432, fg.width, "adaptive layers are 432 px (108 dp at xxxhdpi)");
            Assert.AreEqual(512, legacy.width, "the Play listing icon is 512 px");

            var android = NamedBuildTarget.Android;
            PlatformIcon[] adaptive = PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Adaptive);
            Assert.IsNotEmpty(adaptive);
            foreach (PlatformIcon icon in adaptive)
            {
                Texture2D[] layers = icon.GetTextures();
                Assert.AreEqual(2, layers.Length);
                Assert.IsNotNull(layers[0], "adaptive background");
                Assert.IsNotNull(layers[1], "adaptive foreground");
            }
            foreach (PlatformIcon icon in PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Legacy))
                Assert.IsNotNull(icon.GetTexture(), "legacy icon slot");
        }

        [Test]
        public void SplashIsOursAlone()
        {
            Assert.IsTrue(PlayerSettings.SplashScreen.show);
            Assert.IsFalse(PlayerSettings.SplashScreen.showUnityLogo, "Unity 6 lets the Unity logo go");
            PlayerSettings.SplashScreenLogo[] logos = PlayerSettings.SplashScreen.logos;
            Assert.AreEqual(1, logos.Length);
            Assert.IsNotNull(logos[0].logo, "splash_logo.png as a sprite");
            Assert.That(logos[0].duration, Is.InRange(1.5f, 3f));
        }

        [Test]
        public void PlayerSettingsMatchTheReleaseChecklist()
        {
            var android = NamedBuildTarget.Android;
            Assert.AreEqual("com.mikoch81.nightcafe", PlayerSettings.GetApplicationIdentifier(android));
            Assert.AreEqual("1.0.0", PlayerSettings.bundleVersion);
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(android));
            Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures);
            Assert.IsTrue(PlayerSettings.stripEngineCode);
            Assert.AreEqual(26, (int)PlayerSettings.Android.minSdkVersion);
            Assert.GreaterOrEqual((int)PlayerSettings.Android.targetSdkVersion, 35, "Play's current target requirement");
            Assert.IsFalse(PlayerSettings.Android.useCustomKeystore, "the upload key is set for the release build only, never saved");
            Assert.IsEmpty(PlayerSettings.Android.keystoreName);
        }
    }
}
