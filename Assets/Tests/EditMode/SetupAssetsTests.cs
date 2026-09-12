using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NightCafe.Tests
{
    /// <summary>
    /// Guards for what the scene generator is supposed to leave on disk. These exist because
    /// M2 shipped with an empty volume profile and no one noticed: nothing throws when a
    /// post-processing effect is simply absent.
    /// </summary>
    public sealed class SetupAssetsTests
    {
        const string VolumeProfilePath = "Assets/Settings/NightCafeVolume.asset";
        const string ScenePath = "Assets/Scenes/Game.unity";

        [Test]
        public void VolumeProfileCarriesTheGddBloom()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            Assert.IsNotNull(profile, "run NightCafe/Build Scene Setup first");

            Assert.IsFalse(profile.components.Exists(c => c == null), "profile has a null component slot");
            Assert.IsTrue(profile.TryGet(out Bloom bloom), "Bloom is missing from the profile");
            Assert.IsTrue(bloom.active);
            Assert.AreEqual(0.15f, bloom.intensity.value, 0.0001f, "GDD 5.2 asks for intensity 0.15");
            Assert.AreEqual(VolumeProfilePath, AssetDatabase.GetAssetPath(bloom),
                "Bloom must be stored inside the profile asset, not only in memory");
        }

        [Test]
        public void SceneVolumeReferencesTheProfile()
        {
            string sceneYaml = System.IO.File.ReadAllText(ScenePath);
            string profileGuid = AssetDatabase.AssetPathToGUID(VolumeProfilePath);

            StringAssert.Contains($"sharedProfile: {{fileID: 11400000, guid: {profileGuid}", sceneYaml,
                "GlobalVolume.sharedProfile is not serialised - Volume.profile does not persist");
        }

        [Test]
        public void ArtTexturesAreCompressedForAndroid()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                Assert.IsTrue(android.overridden, $"{path}: no Android override");
                Assert.AreEqual(TextureImporterFormat.ASTC_6x6, android.format, $"{path}: expected ASTC 6x6");
                Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, $"{path}: expected bilinear");
            }
        }
    }
}
