using System.IO;
using NightCafe.Config;
using NightCafe.Core;
using NightCafe.Gameplay;
using NightCafe.InputLayer;
using NightCafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightCafe.EditorTools
{
    /// <summary>
    /// Builds the Milestone 1 assets and Game scene from code so the setup is reproducible
    /// and reviewable, rather than hand-edited scene YAML.
    /// </summary>
    public static class NightCafeSetup
    {
        const string SettingsDir = "Assets/Settings";
        const string PrefabDir = "Assets/Prefabs";
        const string SceneDir = "Assets/Scenes";
        const string ModeConfigPath = SettingsDir + "/ModeConfig_A.asset";
        const string LaneConfigPath = SettingsDir + "/LaneConfig.asset";
        const string CupPrefabPath = PrefabDir + "/Cup.prefab";
        const string GameScenePath = SceneDir + "/Game.unity";

        static readonly Color ScreenBackground = new(0.071f, 0.051f, 0.035f); // #120d09
        static readonly Color ActiveAmber = new(1f, 0.788f, 0.4f);            // #ffc966
        static readonly Color InactiveAmber = new(0.227f, 0.173f, 0.094f);    // #3a2c18

        /// <summary>
        /// Imports the TextMeshPro essential resources and exits once the import finishes.
        /// AssetDatabase.ImportPackage is asynchronous, so this must run in its own Unity
        /// invocation *without* -quit, otherwise the editor exits mid-import.
        /// </summary>
        public static void ImportTextMeshProEssentials()
        {
            if (TMP_Settings.instance != null)
            {
                Debug.Log("[NightCafe] TMP essentials already present.");
                EditorApplication.Exit(0);
                return;
            }

            AssetDatabase.importPackageCompleted += OnImported;
            AssetDatabase.importPackageFailed += OnFailed;
            TMP_PackageResourceImporter.ImportResources(true, false, false);

            void OnImported(string packageName)
            {
                Debug.Log($"[NightCafe] Imported TMP package: {packageName}");
                AssetDatabase.Refresh();
                EditorApplication.Exit(0);
            }

            void OnFailed(string packageName, string error)
            {
                Debug.LogError($"[NightCafe] TMP import failed: {packageName} - {error}");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("NightCafe/Build Milestone 1 Setup")]
        public static void BuildAll()
        {
            ConfigureSpriteImporters();
            ModeConfig modeConfig = CreateModeConfig();
            LaneConfig laneConfig = CreateLaneConfig();
            CreateCupPrefab(laneConfig);
            BuildGameScene(modeConfig, laneConfig);
            ApplyProjectSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NightCafe] Milestone 1 setup complete.");
        }

        /// <summary>
        /// Neo-LCD art wants crisp pixels: point filtering, no compression, centred pivots at 100 PPU.
        /// </summary>
        static void ConfigureSpriteImporters()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                TextureImporterSettings settings = new();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
            }
        }

        static ModeConfig CreateModeConfig()
        {
            EnsureFolder(SettingsDir);
            var config = AssetDatabase.LoadAssetAtPath<ModeConfig>(ModeConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ModeConfig>();
                AssetDatabase.CreateAsset(config, ModeConfigPath);
            }

            EditorUtility.SetDirty(config);
            return config;
        }

        static LaneConfig CreateLaneConfig()
        {
            EnsureFolder(SettingsDir);
            var config = AssetDatabase.LoadAssetAtPath<LaneConfig>(LaneConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LaneConfig>();
                AssetDatabase.CreateAsset(config, LaneConfigPath);
            }

            config.AutoGenerateSteps();
            EditorUtility.SetDirty(config);
            return config;
        }

        static void CreateCupPrefab(LaneConfig laneConfig)
        {
            EnsureFolder(PrefabDir);

            var root = new GameObject("Cup");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite("Assets/Art/sprites/cup.png");
            renderer.color = ActiveAmber;
            renderer.sortingOrder = 10;
            root.AddComponent<CupController>();
            root.transform.localScale = Vector3.one * laneConfig.cupScale;

            PrefabUtility.SaveAsPrefabAsset(root, CupPrefabPath);
            Object.DestroyImmediate(root);
        }

        static void BuildGameScene(ModeConfig modeConfig, LaneConfig laneConfig)
        {
            EnsureFolder(SceneDir);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load the prefab after the scene switch: handles taken before it are invalidated,
            // which silently serialises as a null reference.
            var cupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CupPrefabPath)
                .GetComponent<CupController>();

            // --- Camera -------------------------------------------------------
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.46f; // half of screen_bg height (8.92 world units)
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ScreenBackground;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraGo.AddComponent<AudioListener>();

            // --- Static screen art --------------------------------------------
            var screenRoot = new GameObject("Screen");
            var background = new GameObject("ScreenBG");
            background.transform.SetParent(screenRoot.transform);
            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = LoadSprite("Assets/Art/screen/screen_bg.png");
            backgroundRenderer.sortingOrder = 0;

            Sprite machineHead = LoadSprite("Assets/Art/sprites/machine_head.png");
            foreach (LanePosition lane in System.Enum.GetValues(typeof(LanePosition)))
            {
                var head = new GameObject($"MachineHead_{lane}");
                head.transform.SetParent(screenRoot.transform);
                head.transform.localPosition = laneConfig.GetStart(lane);
                head.transform.localScale = new Vector3(
                    laneConfig.machineHeadScale * (lane.IsLeft() ? 1f : -1f),
                    laneConfig.machineHeadScale,
                    1f);

                var headRenderer = head.AddComponent<SpriteRenderer>();
                headRenderer.sprite = machineHead;
                headRenderer.color = InactiveAmber;
                headRenderer.sortingOrder = 5;
            }

            // --- Barista ------------------------------------------------------
            var baristaGo = new GameObject("Barista");
            var baristaRenderer = baristaGo.AddComponent<SpriteRenderer>();
            baristaRenderer.sprite = LoadSprite("Assets/Art/sprites/barista_up.png");
            baristaRenderer.color = ActiveAmber;
            baristaRenderer.sortingOrder = 20;
            baristaGo.transform.localScale = Vector3.one * laneConfig.baristaScale;
            var barista = baristaGo.AddComponent<PlayerPositionController>();

            SetSerialized(barista, so =>
            {
                so.FindProperty("spriteRenderer").objectReferenceValue = baristaRenderer;
                so.FindProperty("trayUp").objectReferenceValue = LoadSprite("Assets/Art/sprites/barista_up.png");
                so.FindProperty("trayDown").objectReferenceValue = LoadSprite("Assets/Art/sprites/barista_down.png");
                so.FindProperty("catchPose").objectReferenceValue = LoadSprite("Assets/Art/sprites/barista_catch.png");
                so.FindProperty("missPose").objectReferenceValue = LoadSprite("Assets/Art/sprites/barista_miss.png");
            });

            // --- Cups + FX ----------------------------------------------------
            var cupRoot = new GameObject("CupPoolRoot");

            var fxRoot = new GameObject("FX");
            Sprite brokenCup = LoadSprite("Assets/Art/sprites/cup_broken.png");
            var brokenFx = new TimedSpriteFx[3];
            for (int i = 0; i < brokenFx.Length; i++)
            {
                var fxGo = new GameObject($"BrokenCup_{i}");
                fxGo.transform.SetParent(fxRoot.transform);
                fxGo.transform.localScale = Vector3.one * laneConfig.brokenCupScale;

                var fxRenderer = fxGo.AddComponent<SpriteRenderer>();
                fxRenderer.sprite = brokenCup;
                fxRenderer.color = ActiveAmber;
                fxRenderer.sortingOrder = 15;
                fxRenderer.enabled = false;

                var fx = fxGo.AddComponent<TimedSpriteFx>();
                SetSerialized(fx, so => so.FindProperty("spriteRenderer").objectReferenceValue = fxRenderer);
                brokenFx[i] = fx;
            }

            // --- HUD ----------------------------------------------------------
            HudView hud = BuildHud(out _);

            // --- Context ------------------------------------------------------
            var context = new GameObject("GameContext");
            var spawner = context.AddComponent<LaneSpawner>();
            var laneInput = context.AddComponent<LaneInput>();
            var loop = context.AddComponent<GameLoopController>();

            SetSerialized(spawner, so =>
            {
                so.FindProperty("cupPrefab").objectReferenceValue = cupPrefab;
                so.FindProperty("cupRoot").objectReferenceValue = cupRoot.transform;
                so.FindProperty("poolWarmCount").intValue = 4;
            });

            SetSerialized(loop, so =>
            {
                so.FindProperty("modeConfig").objectReferenceValue = modeConfig;
                so.FindProperty("laneConfig").objectReferenceValue = laneConfig;
                so.FindProperty("spawner").objectReferenceValue = spawner;
                so.FindProperty("laneInput").objectReferenceValue = laneInput;
                so.FindProperty("barista").objectReferenceValue = barista;
                so.FindProperty("hud").objectReferenceValue = hud;

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

        static HudView BuildHud(out Canvas canvas)
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            TMP_Text score = CreateText(canvasGo.transform, "ScoreText", "000", 96f,
                new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(400f, 130f));

            var stainRoot = new GameObject("StainIcons", typeof(RectTransform));
            stainRoot.transform.SetParent(canvasGo.transform, false);
            var stainRect = (RectTransform)stainRoot.transform;
            Anchor(stainRect, new Vector2(0.5f, 1f), new Vector2(-360f, -95f), new Vector2(240f, 80f));

            Sprite stainSprite = LoadSprite("Assets/Art/sprites/stain.png");
            var stains = new Image[3];
            for (int i = 0; i < stains.Length; i++)
            {
                var iconGo = new GameObject($"Stain_{i}", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(stainRoot.transform, false);
                Anchor((RectTransform)iconGo.transform, new Vector2(0.5f, 0.5f),
                    new Vector2((i - 1) * 80f, 0f), new Vector2(70f, 45f));

                stains[i] = iconGo.GetComponent<Image>();
                stains[i].sprite = stainSprite;
                stains[i].color = InactiveAmber;
            }

            var titlePanel = new GameObject("TitlePanel", typeof(RectTransform));
            titlePanel.transform.SetParent(canvasGo.transform, false);
            Anchor((RectTransform)titlePanel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 300f));
            CreateText(titlePanel.transform, "TitleText", "NIGHT CAFÉ\nTAP TO START", 64f,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 300f));

            var gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform));
            gameOverPanel.transform.SetParent(canvasGo.transform, false);
            Anchor((RectTransform)gameOverPanel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 400f));
            TMP_Text gameOverText = CreateText(gameOverPanel.transform, "GameOverText",
                "END OF SHIFT\n000\nTAP TO RESTART", 56f,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 400f));
            gameOverPanel.SetActive(false);

            TMP_Text fps = CreateText(canvasGo.transform, "FpsText", "-- fps", 32f,
                new Vector2(1f, 0f), new Vector2(-140f, 50f), new Vector2(240f, 60f));

            var hud = canvasGo.AddComponent<HudView>();
            SetSerialized(hud, so =>
            {
                so.FindProperty("scoreText").objectReferenceValue = score;
                so.FindProperty("titlePanel").objectReferenceValue = titlePanel;
                so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
                so.FindProperty("gameOverText").objectReferenceValue = gameOverText;
                so.FindProperty("fpsText").objectReferenceValue = fps;

                SerializedProperty icons = so.FindProperty("stainIcons");
                icons.arraySize = stains.Length;
                for (int i = 0; i < stains.Length; i++)
                    icons.GetArrayElementAtIndex(i).objectReferenceValue = stains[i];
            });

            return hud;
        }

        static TMP_Text CreateText(Transform parent, string name, string content, float fontSize,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Anchor((RectTransform)go.transform, anchor, position, size);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = ActiveAmber;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        static void Anchor(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        static void ApplyProjectSettings()
        {
            PlayerSettings.productName = "Night Café";
            PlayerSettings.companyName = "mikoch81";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mikoch81.nightcafe");

            // Landscape only (GDD): keep auto-rotation but drop both portrait orientations.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // ARM64 is already the target architecture, which requires IL2CPP.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            AssetDatabase.SaveAssets();
        }

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[NightCafe] Missing sprite: {path}");

            return sprite;
        }

        static void SetSerialized(Object target, System.Action<SerializedObject> configure)
        {
            var so = new SerializedObject(target);
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
