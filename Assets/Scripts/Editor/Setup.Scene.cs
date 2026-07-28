using System;
using System.IO;
using NightCafe.Audio;
using NightCafe.Config;
using NightCafe.Core;
using NightCafe.Gameplay;
using NightCafe.InputLayer;
using NightCafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightCafe.EditorTools
{
    public static partial class NightCafeSetup
    {
        static void BuildGameScene(ModeConfig modeConfig, LaneConfig laneConfig,
            DeviceConfig deviceConfig, AudioConfig audioConfig, TMP_FontAsset monoFont)
        {
            EnsureFolder(SceneDir);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Asset handles taken before the scene switch are invalidated and would silently
            // serialise as null references, so everything is loaded from here on.
            var cupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CupPrefabPath)
                .GetComponent<CupController>();
            var volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);

            Camera camera = BuildCamera(volumeProfile);
            DeviceShellView deviceShell = BuildDevice(deviceConfig);

            Transform screenRoot = Child("ScreenRoot", null,
                new Vector2(0f, deviceConfig.screenOffsetY), deviceConfig.screenScale).transform;

            BuildScreenArt(screenRoot, laneConfig);

            Transform cupRoot = Child("CupPoolRoot", screenRoot).transform;
            TimedSpriteFx[] brokenFx = BuildBrokenCupFx(screenRoot, laneConfig);
            PlayerPositionController barista = BuildBarista(screenRoot, laneConfig);
            CatCrossingView cat = BuildCat(screenRoot, laneConfig);
            StainStripView stains = BuildStains(screenRoot, laneConfig);
            (FlashFx neon, FlashFx dim) = BuildScreenFx(screenRoot);
            (HudView hud, TitleToggleView toggles) = BuildHud(screenRoot, monoFont);

            var context = new GameObject("GameContext");
            var spawner = context.AddComponent<LaneSpawner>();
            var laneInput = context.AddComponent<LaneInput>();
            AudioService audioService = BuildAudio(context, audioConfig);
            var loop = context.AddComponent<GameLoopController>();

            SetSerialized(spawner, so =>
            {
                so.FindProperty("cupPrefab").objectReferenceValue = cupPrefab;
                so.FindProperty("cupRoot").objectReferenceValue = cupRoot;
                so.FindProperty("poolWarmCount").intValue = 4;
            });

            SetSerialized(loop, so =>
            {
                so.FindProperty("modeConfig").objectReferenceValue = modeConfig;
                so.FindProperty("laneConfig").objectReferenceValue = laneConfig;
                so.FindProperty("audioConfig").objectReferenceValue = audioConfig;
                so.FindProperty("spawner").objectReferenceValue = spawner;
                so.FindProperty("laneInput").objectReferenceValue = laneInput;
                so.FindProperty("barista").objectReferenceValue = barista;
                so.FindProperty("hud").objectReferenceValue = hud;
                so.FindProperty("deviceShell").objectReferenceValue = deviceShell;
                so.FindProperty("stainStrip").objectReferenceValue = stains;
                so.FindProperty("cat").objectReferenceValue = cat;
                so.FindProperty("neonFlash").objectReferenceValue = neon;
                so.FindProperty("screenDim").objectReferenceValue = dim;
                so.FindProperty("titleToggles").objectReferenceValue = toggles;
                so.FindProperty("audioService").objectReferenceValue = audioService;
                so.FindProperty("worldCamera").objectReferenceValue = camera;

                SerializedProperty fxArray = so.FindProperty("brokenCupFx");
                fxArray.arraySize = brokenFx.Length;
                for (int i = 0; i < brokenFx.Length; i++)
                    fxArray.GetArrayElementAtIndex(i).objectReferenceValue = brokenFx[i];
            });

            EditorSceneManager.SaveScene(scene, GameScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScenePath, true) };

            string sampleScene = SceneDir + "/SampleScene.unity";
            if (File.Exists(sampleScene))
                AssetDatabase.DeleteAsset(sampleScene);
        }

        static Camera BuildCamera(VolumeProfile profile)
        {
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.278f; // CameraFramer recomputes this for the real aspect
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = WoodBackground;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<CameraFramer>();

            var cameraData = cameraGo.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (cameraData == null)
                cameraData = cameraGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;

            var volumeGo = new GameObject("GlobalVolume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.profile = profile;

            return camera;
        }

        static DeviceShellView BuildDevice(DeviceConfig config)
        {
            Transform device = Child("Device", null).transform;

            var shell = Child("Shell", device, Vector2.zero, config.shellScale);
            var shellRenderer = shell.AddComponent<SpriteRenderer>();
            shellRenderer.sprite = LoadSprite("Assets/Art/device/device_shell.png");
            SetSorting(shellRenderer, Core.SortingLayers.DeviceShell, 0);

            var track = Child("LeverTrack", device, config.leverTrack, config.leverTrackScale);
            var trackRenderer = track.AddComponent<SpriteRenderer>();
            trackRenderer.sprite = LoadSprite("Assets/Art/device/lever_track.png");
            SetSorting(trackRenderer, Core.SortingLayers.DeviceShell, 10);

            var knob = Child("LeverKnob", device, config.leverKnob, config.leverKnobScale);
            var knobRenderer = knob.AddComponent<SpriteRenderer>();
            knobRenderer.sprite = LoadSprite("Assets/Art/device/lever_knob.png");
            SetSorting(knobRenderer, Core.SortingLayers.DeviceShell, 20);

            Transform buttonRoot = Child("Buttons", device).transform;
            Sprite normal = LoadSprite("Assets/Art/device/button_normal.png");
            Sprite pressed = LoadSprite("Assets/Art/device/button_pressed.png");

            var renderers = new SpriteRenderer[LanePositionExtensions.Count];
            foreach (LanePosition lane in Enum.GetValues(typeof(LanePosition)))
            {
                var button = Child($"Button_{lane}", buttonRoot, config.ButtonPosition(lane), config.buttonScale);
                var renderer = button.AddComponent<SpriteRenderer>();
                renderer.sprite = normal;
                SetSorting(renderer, Core.SortingLayers.DeviceShell, 30);
                renderers[(int)lane] = renderer;
            }

            var view = buttonRoot.gameObject.AddComponent<DeviceShellView>();
            SetSerialized(view, so =>
            {
                so.FindProperty("normal").objectReferenceValue = normal;
                so.FindProperty("pressed").objectReferenceValue = pressed;
                so.FindProperty("litDuration").floatValue = config.buttonLitDuration;

                SerializedProperty array = so.FindProperty("buttons");
                array.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            });

            return view;
        }

        static void BuildScreenArt(Transform screenRoot, LaneConfig laneConfig)
        {
            var background = Child("ScreenBG", screenRoot);
            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = LoadSprite("Assets/Art/screen/screen_bg.png");
            SetSorting(backgroundRenderer, Core.SortingLayers.ScreenGlass, 0);

            Transform heads = Child("MachineHeads", screenRoot).transform;
            Sprite machineHead = LoadSprite("Assets/Art/sprites/machine_head.png");
            foreach (LanePosition lane in Enum.GetValues(typeof(LanePosition)))
            {
                var head = Child($"MachineHead_{lane}", heads, laneConfig.GetStart(lane));
                head.transform.localScale = new Vector3(
                    laneConfig.machineHeadScale * (lane.IsLeft() ? 1f : -1f),
                    laneConfig.machineHeadScale,
                    1f);

                var renderer = head.AddComponent<SpriteRenderer>();
                renderer.sprite = machineHead;
                renderer.color = InactiveAmber;
                SetSorting(renderer, Core.SortingLayers.Segments, 0);
            }
        }

        static TimedSpriteFx[] BuildBrokenCupFx(Transform screenRoot, LaneConfig laneConfig)
        {
            Transform fxRoot = Child("FX", screenRoot).transform;
            Sprite brokenCup = LoadSprite("Assets/Art/sprites/cup_broken.png");

            var result = new TimedSpriteFx[3];
            for (int i = 0; i < result.Length; i++)
            {
                var go = Child($"BrokenCup_{i}", fxRoot, Vector2.zero, laneConfig.brokenCupScale);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = brokenCup;
                renderer.color = ActiveAmber;
                renderer.enabled = false;
                SetSorting(renderer, Core.SortingLayers.Segments, 25);

                var fx = go.AddComponent<TimedSpriteFx>();
                SetSerialized(fx, so => so.FindProperty("spriteRenderer").objectReferenceValue = renderer);
                result[i] = fx;
            }

            return result;
        }

        static PlayerPositionController BuildBarista(Transform screenRoot, LaneConfig laneConfig)
        {
            var go = Child("Barista", screenRoot, Vector2.zero, laneConfig.baristaScale);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite("Assets/Art/sprites/barista_up.png");
            renderer.color = ActiveAmber;
            SetSorting(renderer, Core.SortingLayers.Segments, 30);

            var barista = go.AddComponent<PlayerPositionController>();
            SetSerialized(barista, so =>
            {
                so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
                so.FindProperty("trayUp").objectReferenceValue = LoadSprite("Assets/Art/sprites/barista_up.png");
                so.FindProperty("trayDown").objectReferenceValue = LoadSprite("Assets/Art/sprites/barista_down.png");
                so.FindProperty("catchPose").objectReferenceValue = LoadSprite("Assets/Art/sprites/barista_catch.png");
                so.FindProperty("missPose").objectReferenceValue = LoadSprite("Assets/Art/sprites/barista_miss.png");
            });

            return barista;
        }

        static CatCrossingView BuildCat(Transform screenRoot, LaneConfig laneConfig)
        {
            var go = Child("Cat", screenRoot, new Vector2(-6.6f, laneConfig.barLineY), laneConfig.catScale);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite("Assets/Art/sprites/cat_a.png");
            renderer.color = ActiveAmber;
            renderer.enabled = false;
            SetSorting(renderer, Core.SortingLayers.Segments, 10);

            var cat = go.AddComponent<CatCrossingView>();
            SetSerialized(cat, so =>
            {
                so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
                so.FindProperty("frameA").objectReferenceValue = LoadSprite("Assets/Art/sprites/cat_a.png");
                so.FindProperty("frameB").objectReferenceValue = LoadSprite("Assets/Art/sprites/cat_b.png");
                so.FindProperty("y").floatValue = laneConfig.barLineY;
                so.FindProperty("startX").floatValue = -6.8f;
                so.FindProperty("endX").floatValue = 6.8f;
            });

            return cat;
        }

        static StainStripView BuildStains(Transform screenRoot, LaneConfig laneConfig)
        {
            Transform root = Child("Stains", screenRoot).transform;
            Sprite stainSprite = LoadSprite("Assets/Art/sprites/stain.png");

            var renderers = new SpriteRenderer[3];
            for (int i = 0; i < renderers.Length; i++)
            {
                var go = Child($"Stain_{i}", root,
                    new Vector2(-5.0f + i * 0.95f, laneConfig.barLineY), laneConfig.stainScale);

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = stainSprite;
                renderer.color = InactiveAmber;
                SetSorting(renderer, Core.SortingLayers.Segments, 5);
                renderers[i] = renderer;
            }

            var view = root.gameObject.AddComponent<StainStripView>();
            SetSerialized(view, so =>
            {
                so.FindProperty("activeColor").colorValue = ActiveAmber;
                so.FindProperty("inactiveColor").colorValue = InactiveAmber;

                SerializedProperty array = so.FindProperty("stains");
                array.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            });

            return view;
        }

        static (FlashFx neon, FlashFx dim) BuildScreenFx(Transform screenRoot)
        {
            Transform root = Child("ScreenFx", screenRoot).transform;

            var neonGo = Child("NeonFlash", root, new Vector2(0f, -3.68f));
            neonGo.transform.localScale = new Vector3(6.40f, 0.16f, 1f);
            var neonRenderer = neonGo.AddComponent<SpriteRenderer>();
            neonRenderer.sprite = WhitePixelSprite();
            neonRenderer.color = new Color(BrightAmber.r, BrightAmber.g, BrightAmber.b, 0f);
            neonRenderer.enabled = false;
            SetSorting(neonRenderer, Core.SortingLayers.ScreenFx, 0);
            var neon = neonGo.AddComponent<FlashFx>();
            SetSerialized(neon, so => so.FindProperty("spriteRenderer").objectReferenceValue = neonRenderer);

            var dimGo = Child("Dim", root);
            dimGo.transform.localScale = new Vector3(12.72f, 8.92f, 1f);
            var dimRenderer = dimGo.AddComponent<SpriteRenderer>();
            dimRenderer.sprite = WhitePixelSprite();
            dimRenderer.color = new Color(GlassBlack.r, GlassBlack.g, GlassBlack.b, 0f);
            dimRenderer.enabled = false;
            SetSorting(dimRenderer, Core.SortingLayers.ScreenFx, 10);
            var dim = dimGo.AddComponent<FlashFx>();
            SetSerialized(dim, so => so.FindProperty("spriteRenderer").objectReferenceValue = dimRenderer);

            return (neon, dim);
        }

        static AudioService BuildAudio(GameObject context, AudioConfig config)
        {
            var sfxGo = Child("SfxSource", context.transform);
            var sfxSource = sfxGo.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            var musicGo = Child("MusicSource", context.transform);
            var musicSource = musicGo.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;

            var service = context.AddComponent<AudioService>();
            SetSerialized(service, so =>
            {
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("sfxSource").objectReferenceValue = sfxSource;
                so.FindProperty("musicSource").objectReferenceValue = musicSource;
            });

            return service;
        }

        /// <summary>A 1x1 white sprite for the flash and dim quads; scaled to size by the transform.</summary>
        static Sprite WhitePixelSprite()
        {
            const string path = SettingsDir + "/WhitePixel.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 1f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
