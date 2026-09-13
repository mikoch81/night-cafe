using System.IO;
using NightCafe.Config;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NightCafe.EditorTools
{
    public static partial class NightCafeSetup
    {
        const string FontDir = "Assets/Fonts";
        const string MonoFontPath = FontDir + "/LiberationMono-Bold.ttf";
        const string MonoFontAssetPath = FontDir + "/LiberationMono-Bold SDF.asset";
        const string Segment14FontPath = FontDir + "/DSEG14Classic-Regular.ttf";
        const string Segment14FontAssetPath = FontDir + "/DSEG14Classic-Regular SDF.asset";
        const string Segment7FontPath = FontDir + "/DSEG7Classic-Regular.ttf";
        const string Segment7FontAssetPath = FontDir + "/DSEG7Classic-Regular SDF.asset";

        /// <summary>
        /// DSEG (OFL, Assets/Fonts) for what a real LCD shows on segment displays. Digits-only
        /// fields (score, clock) use the 7-segment face - its zero has no slash; fields with
        /// letters (best, mode) use the 14-segment one. Prose stays in the mono font.
        /// </summary>
        static TMP_FontAsset Segment14Font;
        static TMP_FontAsset Segment7Font;

        /// <summary>
        /// Mode A is exactly the field initialisers in ModeConfig, which are the GDD numbers, so
        /// the asset is reset to them on every run: a hand edit in the Inspector must not become
        /// a silent, permanent deviation from the GDD. ModeConfigAssetsTests lock this down.
        /// </summary>
        static ModeConfig CreateModeConfig()
        {
            var config = ResetToDefaults<ModeConfig>(ModeConfigPath);
            config.ordersEnabled = false;
            EditorUtility.SetDirty(config);
            return config;
        }

        /// <summary>
        /// Mode B (GDD 3) shares every Mode A number except the ones the GDD calls out:
        /// orders on, +2 per catch, start at T2, T+1 every 10 catches. Those are re-applied on
        /// every setup run so the asset cannot drift from the GDD; anything else tuned by hand stays.
        /// </summary>
        static ModeConfig CreateModeConfigB()
        {
            var config = ResetToDefaults<ModeConfig>(ModeConfigBPath);
            config.ordersEnabled = true;
            config.pointsPerCatch = 2;
            config.startTempoLevel = 2;
            config.catchesPerTempoLevel = 10;
            config.unlockSkinId = Services.SkinCatalog.OnyxId;
            config.unlockSkinScore = 500;
            EditorUtility.SetDirty(config);
            return config;
        }

        static DeviceConfig CreateDeviceConfig() => ResetToDefaults<DeviceConfig>(DeviceConfigPath);

        const string SegmentAtlasPath = SettingsDir + "/Segments.spriteatlasv2";

        /// <summary>
        /// One atlas for everything on the LCD (Assets/Art/sprites), so the cups, barista, cat and
        /// stains batch into a single draw call. The shell and screen_bg stay loose: they are
        /// large and drawn once each.
        /// </summary>
        static void CreateSpriteAtlas()
        {
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Art/sprites");
            if (folder == null)
            {
                Debug.LogWarning("[NightCafe] Assets/Art/sprites missing; no sprite atlas.");
                return;
            }

            // Rebuilt from scratch every run; Save overwrites the file and the .meta keeps the GUID.
            var atlas = new UnityEditor.U2D.SpriteAtlasAsset();
            atlas.Add(new Object[] { folder });
            atlas.SetIncludeInBuild(true);

            var packing = atlas.GetPackingSettings();
            packing.enableRotation = false;
            packing.enableTightPacking = false; // FullRect sprites keep their padded glow
            packing.padding = 4;
            atlas.SetPackingSettings(packing);

            var texture = atlas.GetTextureSettings();
            texture.filterMode = FilterMode.Bilinear;
            texture.generateMipMaps = false;
            texture.sRGB = true;
            atlas.SetTextureSettings(texture);

            var android = atlas.GetPlatformSettings("Android");
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.maxTextureSize = 2048;
            android.compressionQuality = 100;
            atlas.SetPlatformSettings(android);

            UnityEditor.U2D.SpriteAtlasAsset.Save(atlas, SegmentAtlasPath);
            AssetDatabase.ImportAsset(SegmentAtlasPath);
        }

        static LaneConfig CreateLaneConfig()
        {
            var config = ResetToDefaults<LaneConfig>(LaneConfigPath);
            config.AutoGenerateSteps();
            EditorUtility.SetDirty(config);
            return config;
        }

        /// <summary>
        /// Rewrites a pure-layout asset from the current code defaults, keeping its GUID so scene
        /// references survive. Without this, an asset created by an earlier milestone keeps its
        /// stored values forever and changing a field initialiser has no visible effect - which is
        /// exactly how the barista ended up standing 0.8 units above its slot after the pivot fix.
        /// </summary>
        static T ResetToDefaults<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
                return LoadOrCreate<T>(path);

            var fresh = ScriptableObject.CreateInstance<T>();
            EditorUtility.CopySerialized(fresh, existing);
            Object.DestroyImmediate(fresh);
            existing.name = Path.GetFileNameWithoutExtension(path); // CopySerialized blanks it, and Unity warns on save

            EditorUtility.SetDirty(existing);
            return existing;
        }

        static AudioConfig CreateAudioConfig()
        {
            var config = LoadOrCreate<AudioConfig>(AudioConfigPath);

            config.catchBlip = LoadClip("sfx_catch");
            config.comboArpeggio = LoadClip("sfx_combo");
            config.missClink = LoadClip("sfx_miss");
            config.catMeow = LoadClip("sfx_cat");
            config.gameOver = LoadClip("sfx_gameover");
            config.brewAlarm = LoadClip("sfx_brew_alarm");
            config.leverClick = LoadClip("sfx_click");
            config.lofiLoop = LoadClip("music_lofi_loop");

            EditorUtility.SetDirty(config);
            return config;
        }

        static AudioClip LoadClip(string name)
        {
            string path = $"{AudioDir}/{name}.wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                Debug.LogWarning($"[NightCafe] Missing audio clip: {path} - run tools/gen_audio.py");

            return clip;
        }

        /// <summary>
        /// SFX stay decompressed in memory (they are tiny and must fire without latency);
        /// the minute-long music bed streams as Vorbis so it does not sit in RAM as PCM.
        /// </summary>
        static void ConfigureAudioImporters()
        {
            if (!AssetDatabase.IsValidFolder(AudioDir))
                return;

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
                    continue;

                bool isMusic = Path.GetFileNameWithoutExtension(path).StartsWith("music");

                var settings = importer.defaultSampleSettings;
                settings.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = isMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
                settings.quality = isMusic ? 0.5f : 1f;

                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.loadInBackground = isMusic;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// GDD 5.1 asks for a mono font; TMP's essentials only ship the proportional Liberation Sans.
        /// Liberation Mono is already on this machine under the SIL Open Font License, so it is
        /// copied in rather than downloaded.
        /// </summary>
        /// <summary>
        /// Every character the HUD can show. The atlas is baked static from this set: a dynamic
        /// atlas rewrites the asset whenever play mode meets a new glyph and again when a build
        /// clears it, which is 2 MB of YAML churn per session.
        /// </summary>
        const string HudCharset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÉéÓóŁłŚśŻżŹźĄąĘęĆćŃń·♪░▒▓";

        /// <summary>Digits, the colon and capitals - DSEG has no lowercase or diacritics.</summary>
        const string Segment14Charset = " 0123456789:-ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string Segment7Charset = " 0123456789:-";

        static TMP_FontAsset EnsureSegmentFont(string ttfPath, string assetPath, string baseName, string charset)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                BakeStaticAtlas(existing, charset);
                return existing;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogWarning($"[NightCafe] {baseName} not found in Assets/Fonts; that HUD field falls back to the mono font.");
                return null;
            }

            return CreateFontAsset(font, assetPath, baseName, charset);
        }

        static TMP_FontAsset EnsureMonoFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MonoFontAssetPath);
            if (existing != null)
            {
                BakeStaticAtlas(existing, HudCharset);
                return existing;
            }

            EnsureFolder(FontDir);

            if (!File.Exists(MonoFontPath))
            {
                string[] candidates =
                {
                    "/usr/share/fonts/liberation/LiberationMono-Bold.ttf",
                    "/usr/share/fonts/liberation-fonts/LiberationMono-Bold.ttf",
                    "/usr/share/fonts/truetype/liberation/LiberationMono-Bold.ttf"
                };

                string source = System.Array.Find(candidates, File.Exists);
                if (source == null)
                {
                    Debug.LogWarning("[NightCafe] Liberation Mono not found; HUD falls back to the TMP default font.");
                    return null;
                }

                File.Copy(source, MonoFontPath);
                File.WriteAllText(FontDir + "/LiberationMono-LICENSE.txt",
                    "Liberation Mono is licensed under the SIL Open Font License, Version 1.1.\n" +
                    "https://github.com/liberationfonts/liberation-fonts\n");
                AssetDatabase.ImportAsset(MonoFontPath);
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(MonoFontPath);
            if (font == null)
            {
                Debug.LogWarning("[NightCafe] Liberation Mono failed to import.");
                return null;
            }

            return CreateFontAsset(font, MonoFontAssetPath, "LiberationMono-Bold", HudCharset);
        }

        static TMP_FontAsset CreateFontAsset(Font font, string assetPath, string baseName, string charset)
        {
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
            fontAsset.name = baseName + " SDF";
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // The atlas texture and material are sub-assets and must be stored alongside it.
            if (fontAsset.atlasTexture != null)
            {
                fontAsset.atlasTexture.name = baseName + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = baseName + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[NightCafe] Created TMP font asset {fontAsset.name}.");
            BakeStaticAtlas(fontAsset, charset);
            return fontAsset;
        }

        static void BakeStaticAtlas(TMP_FontAsset fontAsset, string charset)
        {
            if (fontAsset.atlasPopulationMode == AtlasPopulationMode.Static)
                return;

            if (!fontAsset.TryAddCharacters(charset, out string missing))
                Debug.LogWarning($"[NightCafe] Font is missing HUD glyphs: {missing}");

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(fontAsset);
            if (fontAsset.atlasTexture != null)
                EditorUtility.SetDirty(fontAsset.atlasTexture);

            AssetDatabase.SaveAssets();
            Debug.Log("[NightCafe] Baked a static TMP atlas.");
        }

        /// <summary>
        /// GDD 5.2 wants a gentle bloom on the Segments and HUD layers. A URP volume is a
        /// full-screen effect and cannot target sorting layers, so the threshold is set high
        /// enough that only the bright amber art crosses it and the dark wood never does.
        /// </summary>
        static VolumeProfile CreateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            profile.components.RemoveAll(component => component == null); // M2's unsaved Bloom left a null slot

            if (!profile.TryGet(out Bloom bloom))
                bloom = profile.Add<Bloom>(true);

            // VolumeProfile.Add only registers the component in memory; without this it
            // serialises as a null entry and the profile is empty after the next reload.
            // Checked on every run, because an Editor that ran the old setup still holds the
            // in-memory Bloom that never made it to disk.
            if (AssetDatabase.GetAssetPath(bloom) != VolumeProfilePath)
                AssetDatabase.AddObjectToAsset(bloom, profile);

            bloom.active = true;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.15f; // GDD 5.2
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.90f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.65f;
            bloom.tint.overrideState = true;
            bloom.tint.value = ActiveAmber;

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            EnsureFolder(SettingsDir);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
