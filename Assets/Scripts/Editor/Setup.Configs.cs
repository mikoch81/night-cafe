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

        static ModeConfig CreateModeConfig() => LoadOrCreate<ModeConfig>(ModeConfigPath);

        static DeviceConfig CreateDeviceConfig() => ResetToDefaults<DeviceConfig>(DeviceConfigPath);

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
        static TMP_FontAsset EnsureMonoFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MonoFontAssetPath);
            if (existing != null)
                return existing;

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

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
            fontAsset.name = "LiberationMono-Bold SDF";
            AssetDatabase.CreateAsset(fontAsset, MonoFontAssetPath);

            // The atlas texture and material are sub-assets and must be stored alongside it.
            if (fontAsset.atlasTexture != null)
            {
                fontAsset.atlasTexture.name = "LiberationMono-Bold Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = "LiberationMono-Bold Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[NightCafe] Created mono TMP font asset.");
            return fontAsset;
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

            if (!profile.TryGet(out Bloom bloom))
                bloom = profile.Add<Bloom>(true);

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
