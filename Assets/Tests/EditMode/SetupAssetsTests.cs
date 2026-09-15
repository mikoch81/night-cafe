using NightCafe.EditorTools;
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
        public void GlassMaterialUsesTheLcdGlassShader()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/LcdGlass.mat");
            Assert.IsNotNull(material, "run NightCafe/Build Scene Setup first");
            Assert.AreEqual("NightCafe/LcdGlass", material.shader.name);
            Assert.IsFalse(ShaderUtil.ShaderHasError(material.shader), "LcdGlass.shader does not compile");
            Assert.Greater(material.GetFloat("_VignetteStrength"), 0f, "the vignette is switched off");
        }

        [Test]
        public void DeviceModelIsWiredForUnity()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/device/breve_deck.fbx");
            Assert.IsNotNull(model, "run tools/shell_model.py");
            foreach (string part in new[] { "Body", "Screen", "Glass", "Cap_LU", "Cap_LD", "Cap_RU", "Cap_RD", "LeverRail", "LeverKnob" })
                Assert.IsNotNull(model.transform.Find(part), $"{part} missing from the model");

            var importer = (ModelImporter)AssetImporter.GetAtPath("Assets/Art/device/breve_deck.fbx");
            Assert.IsFalse(importer.useFileScale, "model units must be Unity units");
            Assert.IsTrue(importer.isReadable, "the screen face collider needs readable UVs");

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/device/textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var texture = (TextureImporter)AssetImporter.GetAtPath(path);
                bool normal = path.Contains("Normal");
                Assert.AreEqual(normal ? TextureImporterType.NormalMap : TextureImporterType.Default, texture.textureType, path);
                Assert.AreEqual(TextureImporterFormat.ASTC_6x6, texture.GetPlatformTextureSettings("Android").format, path);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/UniversalRenderer.asset"),
                "the 3D renderer asset is missing");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Settings/LcdRT.renderTexture"));
            Assert.IsNotNull(pipeline);
        }

        [Test]
        public void SegmentAtlasPacksEveryLcdSprite()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>("Assets/Settings/Segments.spriteatlasv2");
            Assert.IsNotNull(atlas, "run NightCafe/Build Scene Setup first");
            int loose = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/sprites" }).Length;
            Assert.AreEqual(loose, atlas.spriteCount, "every sprite under Assets/Art/sprites belongs in the atlas");
        }

        [Test]
        public void ArtTexturesAreCompressedForAndroid()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/Art/device/") || AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue; // the 3D shell's maps and HDRI are covered by DeviceModelIsWiredForUnity
                if (path.StartsWith(NightCafeSetup.IconDir + "/"))
                    continue; // launcher icons and the splash logo go to the OS uncompressed (ReleaseSettingsTests)

                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                Assert.IsTrue(android.overridden, $"{path}: no Android override");
                Assert.AreEqual(TextureImporterFormat.ASTC_6x6, android.format, $"{path}: expected ASTC 6x6");
                Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, $"{path}: expected bilinear");
                Assert.IsFalse(importer.mipmapEnabled, $"{path}: mipmaps on an NPOT texture defeat the ASTC override");

                // The override is only real if the imported texture (Android is the active target) is compressed.
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.AreEqual(TextureFormat.ASTC_6x6, texture.format, $"{path}: imported as {texture.format}");
            }
        }
    }
}
