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
        const string VolumeProfilePath = SettingsDir + "/NightCafeVolume.asset";
        const string CupPrefabPath = PrefabDir + "/Cup.prefab";
        const string GameScenePath = SceneDir + "/Game.unity";

        static readonly Color WoodBackground = new(0.329f, 0.188f, 0.102f);   // #54301a
        static readonly Color ActiveAmber = new(1f, 0.788f, 0.4f);            // #ffc966
        static readonly Color BrightAmber = new(1f, 0.824f, 0.478f);          // #ffd27a
        static readonly Color InactiveAmber = new(0.227f, 0.173f, 0.094f);    // #3a2c18
        static readonly Color GlassBlack = new(0.071f, 0.051f, 0.035f);       // #120d09

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
            ConfigureSpriteImporters();
            ConfigureAudioImporters();

            TMP_FontAsset monoFont = EnsureMonoFont();
            ModeConfig[] modeConfigs = { CreateModeConfig(), CreateModeConfigB() };
            LaneConfig laneConfig = CreateLaneConfig();
            DeviceConfig deviceConfig = CreateDeviceConfig();
            AudioConfig audioConfig = CreateAudioConfig();
            CreateVolumeProfile();
            CreateCupPrefab(laneConfig);

            BuildGameScene(modeConfigs, laneConfig, deviceConfig, audioConfig, monoFont);
            ApplyProjectSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NightCafe] Scene setup complete.");
        }

        /// <summary>
        /// Neo-LCD art wants crisp pixels: point filtering, no compression, 100 PPU.
        /// Pivots come from <see cref="SpriteAnchors"/> because the art sits on padded canvases;
        /// note that importer.spritePivot alone is a no-op - the pivot only takes effect through
        /// TextureImporterSettings + SetTextureSettings.
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

                if (SpriteAnchors.TryGet(Path.GetFileNameWithoutExtension(path), out Vector2 pivot))
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = pivot;
                }
                else
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                }

                importer.SetTextureSettings(settings);
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
