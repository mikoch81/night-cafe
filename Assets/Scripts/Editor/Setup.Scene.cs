using System;
using System.IO;
using NightCafe.Audio;
using NightCafe.Config;
using NightCafe.Core;
using NightCafe.Gameplay;
using NightCafe.Services;
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
        static void BuildGameScene(ModeConfig[] modeConfigs, LaneConfig laneConfig,
            DeviceConfig deviceConfig, AudioConfig audioConfig, TMP_FontAsset monoFont)
        {
            EnsureFolder(SceneDir);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Asset handles taken before the scene switch are invalidated and would silently
            // serialise as null references, so everything is loaded from here on.
            var cupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CupPrefabPath)
                .GetComponent<CupController>();
            var volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            var lcdTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(LcdRenderTexturePath);

            // Two cameras: the LCD scene renders into a texture, the device camera looks at the model.
            Camera lcdCamera = BuildLcdCamera(volumeProfile, lcdTexture);
            Camera deviceCamera = BuildDeviceCamera(deviceConfig, UniversalRendererIndex);
            BuildLighting();
            (DeviceShellView deviceShell, MeshCollider screenFace) = BuildDevice(deviceConfig, deviceCamera, lcdTexture);

            // The 2D scene sits at the origin at scale 1: LCD units are world units, and the LCD
            // camera frames screen_bg exactly.
            Transform screenRoot = Child("ScreenRoot", null).transform;

            BuildScreenArt(screenRoot, laneConfig);
            SpriteRenderer[] planks = BuildPlanks(screenRoot, laneConfig);
            SpriteRenderer[] steps = BuildSteps(screenRoot, laneConfig);

            Transform cupRoot = Child("CupPoolRoot", screenRoot).transform;
            TimedSpriteFx[] brokenFx = BuildBrokenCupFx(screenRoot, laneConfig);
            PlayerPositionController barista = BuildBarista(screenRoot, laneConfig);
            CatCrossingView cat = BuildCat(screenRoot, laneConfig, modeConfigs[0]);
            StainStripView stains = BuildStains(screenRoot, laneConfig, modeConfigs[0].maxStains);
            (FlashFx neon, FlashFx dim) = BuildScreenFx(screenRoot);
            SpriteSequenceFx neonCat = BuildNeonCat(screenRoot, laneConfig);
            OrderPanelView orderPanel = BuildOrderPanel(screenRoot, laneConfig, monoFont);
            GameObject ghosts = BuildGhosts(screenRoot, laneConfig);
            (HudView hud, TitleToggleView toggles, ClockWidget clock) = BuildHud(screenRoot, monoFont);
            ScreenStyleApplier styleApplier = BuildStyleApplier(screenRoot, laneConfig, planks, steps, barista, cat, stains, brokenFx, orderPanel);
            var artStyle = AssetDatabase.LoadAssetAtPath<ScreenStyle>(ArtStylePath);
            var retroStyle = AssetDatabase.LoadAssetAtPath<ScreenStyle>(RetroStylePath);

            SetLayerRecursively(screenRoot.gameObject, LayerMask.NameToLayer(LcdLayerName));

            var context = new GameObject("GameContext");
            var spawner = context.AddComponent<LaneSpawner>();
            var laneInput = context.AddComponent<LaneInput>();
            AudioService audioService = BuildAudio(context, audioConfig);
            LcdPointer pointer = BuildPointer(context, deviceCamera, lcdCamera, screenFace);
            var loop = context.AddComponent<GameLoopController>();

            SetSerialized(spawner, so =>
            {
                so.FindProperty("cupPrefab").objectReferenceValue = cupPrefab;
                so.FindProperty("cupRoot").objectReferenceValue = cupRoot;
                so.FindProperty("poolWarmCount").intValue = 4;
            });

            SetSerialized(loop, so =>
            {
                SerializedProperty modes = so.FindProperty("modeConfigs");
                modes.arraySize = modeConfigs.Length;
                for (int i = 0; i < modeConfigs.Length; i++)
                    modes.GetArrayElementAtIndex(i).objectReferenceValue = modeConfigs[i];

                so.FindProperty("laneConfig").objectReferenceValue = laneConfig;
                so.FindProperty("deviceConfig").objectReferenceValue = deviceConfig;
                so.FindProperty("audioConfig").objectReferenceValue = audioConfig;
                so.FindProperty("spawner").objectReferenceValue = spawner;
                so.FindProperty("laneInput").objectReferenceValue = laneInput;
                so.FindProperty("barista").objectReferenceValue = barista;
                so.FindProperty("hud").objectReferenceValue = hud;
                so.FindProperty("clock").objectReferenceValue = clock;
                so.FindProperty("deviceShell").objectReferenceValue = deviceShell;
                so.FindProperty("stainStrip").objectReferenceValue = stains;
                so.FindProperty("cat").objectReferenceValue = cat;
                so.FindProperty("orderPanel").objectReferenceValue = orderPanel;
                so.FindProperty("ghostRoot").objectReferenceValue = ghosts;
                so.FindProperty("styleApplier").objectReferenceValue = styleApplier;
                so.FindProperty("artStyle").objectReferenceValue = artStyle;
                so.FindProperty("retroStyle").objectReferenceValue = retroStyle;
                so.FindProperty("neonFlash").objectReferenceValue = neon;
                so.FindProperty("screenDim").objectReferenceValue = dim;
                so.FindProperty("neonCat").objectReferenceValue = neonCat;
                so.FindProperty("titleToggles").objectReferenceValue = toggles;
                so.FindProperty("audioService").objectReferenceValue = audioService;
                so.FindProperty("pointer").objectReferenceValue = pointer;

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
                so.FindProperty("wipePose").objectReferenceValue = LoadSprite("Assets/Art/sprites/barista_wipe.png");
            });

            return barista;
        }

        static CatCrossingView BuildCat(Transform screenRoot, LaneConfig laneConfig, ModeConfig modeConfig)
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
                so.FindProperty("duration").floatValue = modeConfig.catCrossingDuration;
                so.FindProperty("y").floatValue = laneConfig.barLineY;
                so.FindProperty("startX").floatValue = -6.8f;
                so.FindProperty("endX").floatValue = 6.8f;
            });

            return cat;
        }

        static StainStripView BuildStains(Transform screenRoot, LaneConfig laneConfig, int maxStains)
        {
            Transform root = Child("Stains", screenRoot).transform;
            Sprite stainSprite = LoadSprite("Assets/Art/sprites/stain.png");

            var renderers = new SpriteRenderer[maxStains];
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

            // Glass sits above the flashes and below the game-over dim.
            var glassGo = Child("Glass", root);
            glassGo.transform.localScale = new Vector3(12.72f, 8.92f, 1f);
            var glassRenderer = glassGo.AddComponent<SpriteRenderer>();
            glassRenderer.sprite = WhitePixelSprite();
            glassRenderer.sharedMaterial = CreateGlassMaterial();
            SetSorting(glassRenderer, Core.SortingLayers.ScreenFx, 5);

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

        /// <summary>Rollover 999 (GDD 2.7): six neon_cat frames on the ScreenFX layer.</summary>
        static SpriteSequenceFx BuildNeonCat(Transform screenRoot, LaneConfig laneConfig)
        {
            var go = Child("NeonCat", screenRoot.Find("ScreenFx"), laneConfig.neonCatPosition, laneConfig.neonCatScale);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite("Assets/Art/sprites/neon_cat_6.png");
            renderer.color = BrightAmber;
            renderer.enabled = false;
            SetSorting(renderer, Core.SortingLayers.ScreenFx, 2);

            var fx = go.AddComponent<SpriteSequenceFx>();
            SetSerialized(fx, so =>
            {
                so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
                SerializedProperty frames = so.FindProperty("frames");
                frames.arraySize = 6;
                for (int i = 0; i < 6; i++)
                    frames.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite($"Assets/Art/sprites/neon_cat_{i + 1}.png");
            });
            return fx;
        }

        const string GlassMaterialPath = SettingsDir + "/LcdGlass.mat";

        /// <summary>
        /// The glass material is an asset so the scene can reference it; its numbers come from
        /// PaletteConfig and are pushed in on every run like every other config value.
        /// </summary>
        static Material CreateGlassMaterial()
        {
            Shader shader = Shader.Find("NightCafe/LcdGlass");
            if (shader == null)
            {
                Debug.LogError("[NightCafe] Shader NightCafe/LcdGlass not found.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, GlassMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_VignetteColor", GlassBlack);
            material.SetFloat("_VignetteStrength", Palette.glassVignette);
            material.SetFloat("_VignettePower", Palette.glassVignetteFalloff);
            material.SetColor("_GlareColor", new Color(1f, 0.92f, 0.75f));
            material.SetFloat("_GlareStrength", Palette.glassGlare);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Mode B order light (GDD 3), above the bar on the right - the stains take the left.
        /// The white cup in order_panel.png is at px 56..199 x 48..159 of the 520x208 canvas;
        /// the swatch quad sits over it, slightly inset so the white keeps a thin rim.
        /// </summary>
        static OrderPanelView BuildOrderPanel(Transform screenRoot, LaneConfig laneConfig, TMP_FontAsset monoFont)
        {
            var panel = Child("OrderPanel", screenRoot, laneConfig.orderPanelPosition, laneConfig.orderPanelScale);
            var frame = panel.AddComponent<SpriteRenderer>();
            frame.sprite = LoadSprite("Assets/Art/sprites/order_panel.png");
            frame.enabled = false;
            SetSorting(frame, Core.SortingLayers.Segments, 8);

            var swatchGo = Child("Swatch", panel.transform, new Vector2(-1.325f, 0f));
            swatchGo.transform.localScale = new Vector3(1.30f, 0.98f, 1f);
            var swatch = swatchGo.AddComponent<SpriteRenderer>();
            swatch.sprite = WhitePixelSprite();
            swatch.enabled = false;
            SetSorting(swatch, Core.SortingLayers.Segments, 9);

            Vector2 labelPosition = laneConfig.orderPanelPosition + new Vector2(0.975f * laneConfig.orderPanelScale, 0f);
            TMP_Text label = WorldText("OrderLabel", screenRoot, labelPosition, "ESPRESSO", 3.2f, monoFont,
                ActiveAmber, 0, new Vector2(2.9f * laneConfig.orderPanelScale, 0.8f));
            label.textWrappingMode = TextWrappingModes.NoWrap; // ESPRESSO is wider than the box
            label.overflowMode = TextOverflowModes.Overflow;
            label.gameObject.SetActive(false);

            var view = panel.AddComponent<OrderPanelView>();
            SetSerialized(view, so =>
            {
                so.FindProperty("frame").objectReferenceValue = frame;
                so.FindProperty("swatch").objectReferenceValue = swatch;
                so.FindProperty("label").objectReferenceValue = label;
            });

            return view;
        }

        /// <summary>
        /// GDD 5.2 segment ghosts: every sprite the LCD can show, parked in every slot at 5 %
        /// opacity, the way an unlit segment still shadows through real LCD glass. One static
        /// root toggled by the settings; nothing here moves.
        /// </summary>
        static GameObject BuildGhosts(Transform screenRoot, LaneConfig laneConfig)
        {
            Color ghost = new(ActiveAmber.r, ActiveAmber.g, ActiveAmber.b, laneConfig.ghostAlpha);
            GameObject root = Child("Ghosts", screenRoot);

            Sprite cup = LoadSprite("Assets/Art/sprites/cup.png");
            Sprite brokenCup = LoadSprite("Assets/Art/sprites/cup_broken.png");
            Sprite trayUp = LoadSprite("Assets/Art/sprites/barista_up.png");
            Sprite trayDown = LoadSprite("Assets/Art/sprites/barista_down.png");

            foreach (LanePosition lane in Enum.GetValues(typeof(LanePosition)))
            {
                var steps = laneConfig.GetSteps(lane);
                for (int i = 0; i < steps.Count; i++)
                    Ghost($"Cup_{lane}_{i}", root.transform, steps[i], laneConfig.cupScale, cup, ghost, false);

                Ghost($"Broken_{lane}", root.transform, laneConfig.GetCatchPoint(lane), laneConfig.brokenCupScale,
                    brokenCup, ghost, false);

                Ghost($"Barista_{lane}", root.transform, laneConfig.GetBaristaSlot(lane), laneConfig.baristaScale,
                    lane.IsUp() ? trayUp : trayDown, ghost, lane.IsLeft());
            }

            Ghost("Cat", root.transform, new Vector2(0f, laneConfig.barLineY), laneConfig.catScale,
                LoadSprite("Assets/Art/sprites/cat_a.png"), ghost, false);
            Ghost("OrderPanel", root.transform, laneConfig.orderPanelPosition, laneConfig.orderPanelScale,
                LoadSprite("Assets/Art/sprites/order_panel.png"), ghost, false);

            root.SetActive(false); // GameLoopController switches it on from the saved setting
            return root;
        }

        static void Ghost(string name, Transform parent, Vector2 position, float scale, Sprite sprite,
            Color colour, bool mirrorX)
        {
            var go = Child(name, parent, position, scale);
            if (mirrorX)
                go.transform.localScale = new Vector3(-scale, scale, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = colour;
            SetSorting(renderer, Core.SortingLayers.Segments, -1);
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

            var ambienceGo = Child("AmbienceSource", context.transform);
            var ambienceSource = ambienceGo.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;

            var service = context.AddComponent<AudioService>();
            SetSerialized(service, so =>
            {
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("sfxSource").objectReferenceValue = sfxSource;
                so.FindProperty("musicSource").objectReferenceValue = musicSource;
                so.FindProperty("ambienceSource").objectReferenceValue = ambienceSource;
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
