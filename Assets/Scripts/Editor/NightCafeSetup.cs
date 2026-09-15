using System.IO;
using NightCafe.Config;
using NightCafe.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NightCafe.EditorTools
{
    /// <summary>
    /// Builds the project assets and the Game scene from code so the setup is reproducible
    /// and reviewable, rather than hand-edited scene YAML.
    /// Split across Setup.*.cs partials; this file holds the entry points and shared helpers.
    /// </summary>
    public static partial class NightCafeSetup
    {
        const string SettingsDir = "Assets/Settings";
        const string PrefabDir = "Assets/Prefabs";
        const string SceneDir = "Assets/Scenes";
        const string AudioDir = "Assets/Audio";
        const string ModeConfigPath = SettingsDir + "/ModeConfig_A.asset";
        const string ModeConfigBPath = SettingsDir + "/ModeConfig_B.asset";
        const string LaneConfigPath = SettingsDir + "/LaneConfig.asset";
        const string DeviceConfigPath = SettingsDir + "/DeviceConfig.asset";
        const string AudioConfigPath = SettingsDir + "/AudioConfig.asset";
        const string ArtAudioDir = AudioDir + "/Art";
        const string ArtAudioConfigPath = SettingsDir + "/AudioConfig_Art.asset";
        const string VolumeProfilePath = SettingsDir + "/NightCafeVolume.asset";
        const string CupPrefabPath = PrefabDir + "/Cup.prefab";
        const string GameScenePath = SceneDir + "/Game.unity";

        const string PaletteConfigPath = SettingsDir + "/PaletteConfig.asset";

        // Filled from PaletteConfig at the start of BuildAll; the asset is the single source.
        static Color WoodBackground;
        static Color ActiveAmber;
        static Color BrightAmber;
        static Color InactiveAmber;
        static Color GlassBlack;
        static PaletteConfig Palette;
        static int UniversalRendererIndex;

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

        [MenuItem("NightCafe/Build Scene Setup")]
        public static void BuildAll()
        {
            EnsureSortingLayers(); // before any renderer exists
            EnsureLayers();
            LoadPalette();
            ConfigureSpriteImporters();
            ConfigureDeviceTextures();
            ConfigureEnvironmentTexture();
            ConfigureModelImporter();
            ConfigureAudioImporters();
            UniversalRendererIndex = EnsureUniversalRenderer();

            TMP_FontAsset monoFont = EnsureMonoFont();
            Segment14Font = EnsureSegmentFont(Segment14FontPath, Segment14FontAssetPath, "DSEG14Classic-Regular", Segment14Charset);
            Segment7Font = EnsureSegmentFont(Segment7FontPath, Segment7FontAssetPath, "DSEG7Classic-Regular", Segment7Charset);
            ModeConfig[] modeConfigs = { CreateModeConfig(), CreateModeConfigB() };
            LaneConfig laneConfig = CreateLaneConfig();
            DeviceConfig deviceConfig = CreateDeviceConfig();
            AudioConfig audioConfig = CreateAudioConfig();
            AudioConfig artAudioConfig = CreateArtAudioConfig();
            CreateScreenStyles(artAudioConfig, laneConfig);
            CreateVolumeProfile();
            CreateSpriteAtlas();
            EnsureLcdRenderTexture(deviceConfig);
            CreateCupPrefab(laneConfig);

            BuildGameScene(modeConfigs, laneConfig, deviceConfig, audioConfig, monoFont);
            ApplyProjectSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NightCafe] Scene setup complete.");
        }

        /// <summary>
        /// The art is SVG-rendered at 4x and never shown at an integer scale (the LCD lands at
        /// roughly 1.6x on a 1080p phone), so bilinear filtering reads as the intended soft glow
        /// where point filtering shimmered on the diagonal rails. No mipmaps anywhere: nothing is
        /// ever minified (even the 1920 px shell is magnified on a 1080p phone), and mipmaps on
        /// these non-power-of-two canvases make Unity silently fall back to RGBA32 on Android.
        /// Android gets ASTC: a flat two-colour palette survives 6x6 blocks visually intact, and
        /// it turns ~17 MB of RGBA32 into ~2 MB.
        /// Pivots come from <see cref="SpriteAnchors"/> because the art sits on padded canvases;
        /// note that importer.spritePivot alone is a no-op - the pivot only takes effect through
        /// TextureImporterSettings + SetTextureSettings.
        /// </summary>
        static void ConfigureSpriteImporters()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/Art/device/") || AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue; // the 3D shell's maps and environment are configured in Setup.Device

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                // The painted set is authored at twice the LCD resolution so it stays crisp on the render texture.
                importer.spritePixelsPerUnit = path.StartsWith(ScreenV3Dir + "/") ? 200f : 100f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed; // editor/default
                importer.maxTextureSize = 4096; // device_shell is 2400 px wide

                TextureImporterSettings settings = new();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; // atlas-friendly, no tight-mesh seams on glow

                bool painted = path.StartsWith(ScreenV3Dir + "/");
                string spriteName = Path.GetFileNameWithoutExtension(path);
                // The shelf plank is 9-sliced along the rails: 24 canvas px of end cap, rendered at 4x.
                settings.spriteBorder = painted && spriteName == "plank" ? new Vector4(96f, 0f, 96f, 0f) : Vector4.zero;
                if (SpriteAnchors.TryGet(spriteName, painted, out Vector2 pivot))
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = pivot;
                }
                else
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                }

                importer.SetTextureSettings(settings);

                var android = importer.GetPlatformTextureSettings("Android");
                android.overridden = true;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.compressionQuality = 100;
                android.maxTextureSize = 4096; // device_shell is 2400 px wide
                importer.SetPlatformTextureSettings(android);

                importer.SaveAndReimport();
            }
        }

        static void CreateCupPrefab(LaneConfig laneConfig)
        {
            EnsureFolder(PrefabDir);

            var root = new GameObject("Cup");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite("Assets/Art/sprites/cup.png");
            renderer.color = BrightAmber;
            SetSorting(renderer, Core.SortingLayers.Segments, 20);
            var cup = root.AddComponent<CupController>();
            SetSerialized(cup, so => so.FindProperty("spriteRenderer").objectReferenceValue = renderer);
            root.transform.localScale = Vector3.one * laneConfig.cupScale;

            PrefabUtility.SaveAsPrefabAsset(root, CupPrefabPath);
            Object.DestroyImmediate(root);
        }

        static void LoadPalette()
        {
            var palette = ResetToDefaults<PaletteConfig>(PaletteConfigPath);
            Palette = palette;
            WoodBackground = palette.woodBackground;
            ActiveAmber = palette.activeAmber;
            BrightAmber = palette.brightAmber;
            InactiveAmber = palette.inactiveAmber;
            GlassBlack = palette.glassBlack;
        }

        static void ApplyProjectSettings()
        {
            PlayerSettings.productName = "Night Café";
            PlayerSettings.companyName = "mikoch81";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mikoch81.nightcafe");

            // GameActivity (the Unity 6 default) recreated the activity 60 ms after a cold start
            // on a Pixel 10 and crashed in UnityFoldingFeaturesWrapper.init() about one launch
            // in three. The classic Activity entry point does not have that path.
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;

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

        static GameObject Child(string name, Transform parent, Vector2 localPosition = default, float scale = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * scale;
            return go;
        }
    }
}
