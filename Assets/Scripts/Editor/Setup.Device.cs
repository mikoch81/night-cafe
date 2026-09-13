using System.Linq;
using NightCafe.Config;
using NightCafe.Core;
using NightCafe.Services;
using NightCafe.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NightCafe.EditorTools
{
    /// <summary>
    /// The Bréve Deck as a 3D object (M4.5). The model comes from tools/shell_model.py as an FBX;
    /// this partial imports it, gives it materials and colliders, and sets up the two cameras:
    /// an orthographic one that renders the 2D LCD scene into a texture, and a perspective one
    /// that looks at the device with that texture on its screen face.
    /// </summary>
    public static partial class NightCafeSetup
    {
        public const string LcdLayerName = "LCD";
        public const string DeviceLayerName = "Device";

        const string ModelPath = "Assets/Art/device/breve_deck.fbx";
        const string DeviceTextureDir = "Assets/Art/device/textures";
        const string MaterialDir = SettingsDir + "/Materials";
        const string RendererPath = SettingsDir + "/UniversalRenderer.asset";
        const string UrpAssetPath = SettingsDir + "/UniversalRP.asset";
        const string LcdRenderTexturePath = SettingsDir + "/LcdRT.renderTexture";
        const string EnvironmentPath = "Assets/Art/device/env/studio_small_09_2k.hdr";

        // The FBX arrives with its top face along +Y and both X and Z mirrored (Blender (x, y, z)
        // -> Unity (-x, z, -y)). X first then Z turns the face towards the camera (-Z) with the
        // screen's up along +Y and its right along +X.
        static readonly Quaternion DeviceRootRotation = Quaternion.Euler(0f, 0f, 180f) * Quaternion.Euler(-90f, 0f, 0f);

        // ------------------------------------------------------------------ project-level assets

        /// <summary>
        /// Two user layers: LCD for the 2D scene (and its post-processing volume), Device for the
        /// model. TagManager has no API for layers either, so it is edited like the sorting layers.
        /// </summary>
        static void EnsureLayers()
        {
            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath).FirstOrDefault();
            if (tagManagerAsset == null)
            {
                Debug.LogError($"[NightCafe] Could not load {TagManagerPath}; layers not created.");
                return;
            }

            var tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("layers");
            bool changed = false;
            foreach (string name in new[] { LcdLayerName, DeviceLayerName })
            {
                bool present = false;
                int firstFree = -1;
                for (int i = 8; i < layers.arraySize; i++)
                {
                    string value = layers.GetArrayElementAtIndex(i).stringValue;
                    if (value == name)
                        present = true;
                    else if (string.IsNullOrEmpty(value) && firstFree < 0)
                        firstFree = i;
                }

                if (present || firstFree < 0)
                    continue;

                layers.GetArrayElementAtIndex(firstFree).stringValue = name;
                changed = true;
            }

            if (changed)
            {
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// The project started 2D-only, with a single Renderer2D in the URP asset. The device needs
        /// the standard Universal Renderer (lit materials, shadows), so it is added as renderer 1.
        /// </summary>
        static int EnsureUniversalRenderer()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (pipeline == null)
            {
                Debug.LogError($"[NightCafe] {UrpAssetPath} missing.");
                return 0;
            }

            var so = new SerializedObject(pipeline);
            SerializedProperty list = so.FindProperty("m_RendererDataList");
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer)
                    return i;
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = renderer;
            so.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return list.arraySize - 1;
        }

        /// <summary>The LCD scene renders into this; the screen face shows it. sRGB so the amber matches.</summary>
        static RenderTexture EnsureLcdRenderTexture(DeviceConfig config)
        {
            int height = Mathf.Max(256, config.lcdTextureHeight);
            int width = Mathf.RoundToInt(height * DeviceLayout.LcdWidth / DeviceLayout.LcdHeight);

            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(LcdRenderTexturePath);
            if (texture == null)
            {
                texture = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_SRGB) { name = "LcdRT" };
                AssetDatabase.CreateAsset(texture, LcdRenderTexturePath);
            }
            else if (texture.width != width || texture.height != height)
            {
                texture.Release();
                texture.width = width;
                texture.height = height;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.useMipMap = false;
            EditorUtility.SetDirty(texture);
            return texture;
        }

        /// <summary>Model units are Unity units; materials come from here, not from the file.</summary>
        static void ConfigureModelImporter()
        {
            if (AssetImporter.GetAtPath(ModelPath) is not ModelImporter importer)
            {
                Debug.LogError($"[NightCafe] {ModelPath} missing - run tools/shell_model.py.");
                return;
            }

            // Materials are imported (and then replaced at scene build): with import mode None
            // Unity folds the body's two material slots into a single submesh.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.useFileScale = false;
            importer.globalScale = 1f;
            importer.isReadable = true; // the screen face's MeshCollider reports UVs
            importer.importNormals = ModelImporterNormals.Import;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// The studio HDRI (Poly Haven, CC0) as a cubemap: it is what the aluminium and the glass
        /// reflect, and the soft ambient the wood sits in. 1024 is plenty for reflections.
        /// </summary>
        static void ConfigureEnvironmentTexture()
        {
            if (AssetImporter.GetAtPath(EnvironmentPath) is not TextureImporter importer)
            {
                Debug.LogWarning($"[NightCafe] {EnvironmentPath} missing; no environment reflections.");
                return;
            }

            importer.textureShape = TextureImporterShape.TextureCube;
            importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_HDR_6x6;
            android.maxTextureSize = 512;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }

        /// <summary>Wood and metal maps for the lit materials: colour in sRGB, normals as normal maps, mips on.</summary>
        static void ConfigureDeviceTextures()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { DeviceTextureDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                bool normal = path.Contains("Normal");
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 4;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Compressed;

                var android = importer.GetPlatformTextureSettings("Android");
                android.overridden = true;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.maxTextureSize = 1024;
                android.compressionQuality = 100;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------------ materials

        static Material LitMaterial(string name, Color colour, float metallic, float smoothness,
            string colourMap = null, string normalMap = null, float tiling = 1f, float bumpScale = 1f)
        {
            Material material = MaterialAsset(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Surface", 0f);
            material.SetTexture("_BaseMap", colourMap != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{DeviceTextureDir}/{colourMap}") : null);
            material.SetTexture("_BumpMap", normalMap != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{DeviceTextureDir}/{normalMap}") : null);
            material.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            material.SetFloat("_BumpScale", bumpScale);
            if (normalMap != null)
                material.EnableKeyword("_NORMALMAP");
            else
                material.DisableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material MaterialAsset(string name, string shaderName)
        {
            EnsureFolder(MaterialDir);
            string path = $"{MaterialDir}/{name}.mat";
            Shader shader = Shader.Find(shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            return material;
        }

        static Material CapMaterial()
        {
            Material material = LitMaterial("Cap", new Color(0.80f, 0.74f, 0.62f), 0f, 0.5f);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetColor("_EmissionColor", Color.black); // DeviceShellView drives it per cap
            return material;
        }

        static Material ScreenMaterial(RenderTexture lcd)
        {
            Material material = MaterialAsset("LcdScreen", "Universal Render Pipeline/Unlit");
            material.SetTexture("_BaseMap", lcd);
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material GlassMaterial()
        {
            // Nearly clear; what sells the glass is the environment reflection in its specular,
            // not the tint (a visible tint greys the amber out).
            Material material = LitMaterial("ScreenGlass", new Color(1f, 1f, 1f, 0.03f), 0f, 0.94f);
            // URP Lit transparent: surface type 1, alpha blend, no depth write, render after opaques.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------ scene

        static Camera BuildLcdCamera(VolumeProfile profile, RenderTexture target)
        {
            int lcdLayer = LayerMask.NameToLayer(LcdLayerName);
            int deviceLayer = LayerMask.NameToLayer(DeviceLayerName);

            var cameraGo = new GameObject("LcdCamera") { layer = lcdLayer };
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = DeviceLayout.LcdOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = GlassBlack;
            camera.cullingMask = ~(1 << deviceLayer); // the 2D scene, including cups spawned on Default
            camera.targetTexture = target;
            camera.depth = -1f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var cameraData = cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraData.SetRenderer(0); // Renderer2D
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 1 << lcdLayer;
            cameraData.antialiasing = AntialiasingMode.None;

            var volumeGo = new GameObject("GlobalVolume") { layer = lcdLayer };
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            // sharedProfile is the serialised reference; Volume.profile is a runtime clone that
            // never reaches the saved scene, which is how M2 shipped without any bloom at all.
            volume.sharedProfile = profile;

            return camera;
        }

        static Camera BuildDeviceCamera(DeviceConfig config, int rendererIndex)
        {
            int deviceLayer = LayerMask.NameToLayer(DeviceLayerName);

            var cameraGo = new GameObject("DeviceCamera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = config.cameraFov;
            camera.nearClipPlane = 1f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = WoodBackground;
            camera.cullingMask = 1 << deviceLayer;
            camera.depth = 0f;
            cameraGo.AddComponent<AudioListener>();

            var framer = cameraGo.AddComponent<CameraFramer>();
            SetSerialized(framer, so =>
            {
                so.FindProperty("fieldOfView").floatValue = config.cameraFov;
                so.FindProperty("tiltDegrees").floatValue = config.cameraTiltDegrees;
            });
            var tilt = cameraGo.AddComponent<DeviceTilt>();
            SetSerialized(tilt, so => so.FindProperty("degrees").floatValue = config.parallaxDegrees);

            var cameraData = cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraData.SetRenderer(rendererIndex);
            cameraData.renderPostProcessing = false;
            cameraData.volumeLayerMask = 0;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            cameraData.renderShadows = true;

            return camera;
        }

        static void BuildLighting()
        {
            int deviceLayer = LayerMask.NameToLayer(DeviceLayerName);

            var lightGo = new GameObject("KeyLight") { layer = deviceLayer };
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            light.cullingMask = 1 << deviceLayer;
            // Into the device (+Z), from the upper left of the view.
            lightGo.transform.rotation = Quaternion.LookRotation(new Vector3(0.35f, -0.45f, 0.82f));

            // Environment: the studio HDRI lights the device softly and is what its metal and
            // glass reflect. The camera never shows the sky itself (the counter fills the view).
            var environment = AssetDatabase.LoadAssetAtPath<Cubemap>(EnvironmentPath);
            if (environment != null)
            {
                Material skybox = MaterialAsset("Environment", "Skybox/Cubemap");
                skybox.SetTexture("_Tex", environment);
                skybox.SetFloat("_Exposure", 0.7f);
                EditorUtility.SetDirty(skybox);
                RenderSettings.skybox = skybox;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = 0.35f;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
                RenderSettings.defaultReflectionResolution = 256;
                RenderSettings.reflectionIntensity = 0.55f; // the studio is bright; wood must stay matte
                DynamicGI.UpdateEnvironment();
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.32f, 0.28f, 0.26f);
                RenderSettings.ambientEquatorColor = new Color(0.20f, 0.16f, 0.14f);
                RenderSettings.ambientGroundColor = new Color(0.08f, 0.06f, 0.05f);
            }
        }

        /// <summary>
        /// Instantiates the model under a root that turns its top face towards the camera, wires
        /// the parts DeviceShellView moves, and gives the screen face its collider and texture.
        /// </summary>
        static (DeviceShellView shell, MeshCollider screenFace) BuildDevice(DeviceConfig config, Camera deviceCamera, RenderTexture lcd)
        {
            int deviceLayer = LayerMask.NameToLayer(DeviceLayerName);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError($"[NightCafe] {ModelPath} missing - run tools/shell_model.py.");
                return (null, null);
            }

            var root = new GameObject("Device");
            root.transform.rotation = DeviceRootRotation;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "BreveDeck";
            instance.transform.SetParent(root.transform, false);

            // The counter the device lies on: its shadow needs somewhere to fall. World space,
            // just behind the body's underside; a Quad faces -Z, i.e. the camera.
            var counter = GameObject.CreatePrimitive(PrimitiveType.Quad);
            counter.name = "Counter";
            counter.layer = deviceLayer;
            counter.transform.position = new Vector3(0f, 0f, 0.02f);
            counter.transform.localScale = new Vector3(400f, 400f, 1f);
            counter.GetComponent<Renderer>().sharedMaterial = LitMaterial("Counter", new Color(0.11f, 0.085f, 0.075f), 0f, 0.15f);
            Object.DestroyImmediate(counter.GetComponent<Collider>());

            Material wood = LitMaterial("Wood_Walnut", Color.white, 0f, 0.22f, "Wood027_Color.jpg", "Wood027_NormalGL.jpg", 0.25f, 0.6f);
            Material alu = LitMaterial("Aluminium", new Color(0.86f, 0.86f, 0.86f), 0.9f, 0.62f, "Metal009_Color.jpg", "Metal009_NormalGL.jpg", 0.5f);

            // Skins (GDD 6): the wood top per finish; neon turns the chamfer into a purple tube.
            Material ash = LitMaterial("Wood_Ash", new Color(0.86f, 0.70f, 0.48f), 0f, 0.16f, "Wood095_Color.jpg", "Wood095_NormalGL.jpg", 0.25f, 0.3f);
            Material onyx = LitMaterial("Wood_Onyx", new Color(0.5f, 0.5f, 0.55f), 0f, 0.3f, "Wood028_Color.jpg", "Wood028_NormalGL.jpg", 0.25f, 0.6f);
            Material neonWood = LitMaterial("Wood_Neon", new Color(0.55f, 0.45f, 0.8f), 0f, 0.3f, "Wood028_Color.jpg", "Wood028_NormalGL.jpg", 0.25f, 0.6f);
            Material neonTube = LitMaterial("NeonTube", new Color(0.35f, 0.15f, 0.8f), 0f, 0.25f);
            neonTube.EnableKeyword("_EMISSION");
            neonTube.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            neonTube.SetColor("_EmissionColor", new Color(0.5f, 0.2f, 1.0f) * 3.0f);
            EditorUtility.SetDirty(neonTube);
            var skinMaterials = new (string id, Material top, Material edge)[]
            {
                (SkinCatalog.DefaultId, wood, alu), (SkinCatalog.AshId, ash, alu),
                (SkinCatalog.OnyxId, onyx, alu), (SkinCatalog.NeonId, neonWood, neonTube),
            };
            Material dark = LitMaterial("Dark", new Color(0.05f, 0.04f, 0.035f), 0f, 0.3f);
            Material cap = CapMaterial();
            Material screen = ScreenMaterial(lcd);
            Material glass = GlassMaterial();

            var caps = new Transform[LanePositionExtensions.Count];
            var capRenderers = new Renderer[LanePositionExtensions.Count];
            Renderer bodyRenderer = null;
            int bodyTopSlot = 0, bodyEdgeSlot = 1;
            Transform knob = null;
            Collider lever = null;
            MeshCollider screenFace = null;

            foreach (Transform part in instance.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = deviceLayer;
                var renderer = part.GetComponent<MeshRenderer>();
                if (renderer == null)
                    continue;

                string name = part.name;
                if (name == "Body")
                {
                    // Unity orders the submeshes by the imported material names (Slot_Top /
                    // Slot_Edge from tools/shell_model.py), not by Blender's slot order.
                    Material[] imported = renderer.sharedMaterials;
                    var assigned = new Material[imported.Length];
                    for (int i = 0; i < imported.Length; i++)
                    {
                        bool top = imported[i] != null && imported[i].name.Contains("Top");
                        assigned[i] = top ? wood : alu;
                        if (top) bodyTopSlot = i; else bodyEdgeSlot = i;
                    }
                    renderer.sharedMaterials = assigned;
                    bodyRenderer = renderer;
                }
                else if (name.StartsWith("Collar_") || name == "LeverRail" || name == "LeverKnob")
                    renderer.sharedMaterial = alu;
                else if (name.StartsWith("Cap_"))
                    renderer.sharedMaterial = cap;
                else if (name == "Screen")
                    renderer.sharedMaterial = screen;
                else if (name == "Glass")
                    renderer.sharedMaterial = glass;
                else
                    renderer.sharedMaterial = dark; // wells, slot floor, grille, labels, brand

                renderer.shadowCastingMode = name is "Screen" or "Glass"
                    ? ShadowCastingMode.Off
                    : ShadowCastingMode.On;

                if (name.StartsWith("Cap_") && TryLaneFromSuffix(name.Substring(4), out LanePosition lane))
                {
                    caps[(int)lane] = part;
                    capRenderers[(int)lane] = renderer;
                }
                else if (name == "LeverKnob")
                {
                    knob = part;
                }
                else if (name == "LeverRail")
                {
                    var box = part.gameObject.AddComponent<BoxCollider>();
                    box.size = new Vector3(box.size.x + 0.6f, box.size.y + 0.6f, box.size.z + 0.6f); // a fat finger target
                    lever = box;
                }
                else if (name == "Screen")
                {
                    screenFace = part.gameObject.AddComponent<MeshCollider>();
                    screenFace.sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
                }
            }

            var view = root.AddComponent<DeviceShellView>();
            SetSerialized(view, so =>
            {
                SerializedProperty capArray = so.FindProperty("caps");
                SerializedProperty rendererArray = so.FindProperty("capRenderers");
                capArray.arraySize = caps.Length;
                rendererArray.arraySize = caps.Length;
                for (int i = 0; i < caps.Length; i++)
                {
                    capArray.GetArrayElementAtIndex(i).objectReferenceValue = caps[i];
                    rendererArray.GetArrayElementAtIndex(i).objectReferenceValue = capRenderers[i];
                }

                so.FindProperty("capTravel").floatValue = config.capTravel;
                so.FindProperty("pressSeconds").floatValue = config.capPressSeconds;
                so.FindProperty("releaseSeconds").floatValue = config.capReleaseSeconds;
                so.FindProperty("litFadeSeconds").floatValue = config.capLitFadeSeconds;
                so.FindProperty("leverKnob").objectReferenceValue = knob;
                so.FindProperty("leverCollider").objectReferenceValue = lever;
                so.FindProperty("leverKnobX").floatValue = config.leverKnobX;
                so.FindProperty("leverSlideSeconds").floatValue = config.leverSlideSeconds;
                so.FindProperty("deviceCamera").objectReferenceValue = deviceCamera;
                so.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
                so.FindProperty("bodyTopSlot").intValue = bodyTopSlot;
                so.FindProperty("bodyEdgeSlot").intValue = bodyEdgeSlot;

                SerializedProperty skinArray = so.FindProperty("skins");
                skinArray.arraySize = skinMaterials.Length;
                for (int i = 0; i < skinMaterials.Length; i++)
                {
                    SerializedProperty entry = skinArray.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("id").stringValue = skinMaterials[i].id;
                    entry.FindPropertyRelative("top").objectReferenceValue = skinMaterials[i].top;
                    entry.FindPropertyRelative("edge").objectReferenceValue = skinMaterials[i].edge;
                }
            });

            return (view, screenFace);
        }

        static LcdPointer BuildPointer(GameObject host, Camera deviceCamera, Camera lcdCamera, MeshCollider screenFace)
        {
            var pointer = host.AddComponent<LcdPointer>();
            SetSerialized(pointer, so =>
            {
                so.FindProperty("deviceCamera").objectReferenceValue = deviceCamera;
                so.FindProperty("lcdCamera").objectReferenceValue = lcdCamera;
                so.FindProperty("screenFace").objectReferenceValue = screenFace;
            });
            return pointer;
        }

        /// <summary>Cap_LU / Cap_LD / Cap_RU / Cap_RD, the model's names for the four lanes.</summary>
        static bool TryLaneFromSuffix(string suffix, out LanePosition lane)
        {
            switch (suffix)
            {
                case "LU": lane = LanePosition.LeftUp; return true;
                case "LD": lane = LanePosition.LeftDown; return true;
                case "RU": lane = LanePosition.RightUp; return true;
                case "RD": lane = LanePosition.RightDown; return true;
                default: lane = LanePosition.LeftUp; return false;
            }
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }
    }
}
