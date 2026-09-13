using System.Collections.Generic;
using System.IO;
using NightCafe.Config;
using NightCafe.Core;
using NightCafe.Gameplay;
using NightCafe.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NightCafe.EditorTools
{
    public static partial class NightCafeSetup
    {
        const string ArtStylePath = SettingsDir + "/ScreenStyle_Art.asset";
        const string RetroStylePath = SettingsDir + "/ScreenStyle_Retro.asset";
        public const string ScreenV3Dir = "Assets/Art/screen_v3";

        /// <summary>Files the painted style needs in Assets/Art/screen_v3 (GDD 5.2), by style slot.</summary>
        public static readonly (string slot, string file)[] ArtSprites =
        {
            ("background", "bg.png"), ("plank", "plank.png"), ("machineHead", "machine_head.png"),
            ("baristaUp", "miro_up.png"), ("baristaDown", "miro_down.png"), ("baristaCatch", "miro_catch.png"),
            ("baristaMiss", "miro_miss.png"), ("baristaWipe", "miro_wipe.png"),
            ("catA", "sable_a.png"), ("catB", "sable_b.png"),
            ("cup0", "cup_espresso.png"), ("cup1", "cup_caramel.png"), ("cup2", "cup_latte.png"), ("cup3", "cup_decaf.png"),
            ("cupBroken", "cup_broken.png"), ("stain", "stain.png"), ("orderPanel", "order_panel.png"),
        };

        /// <summary>
        /// The two screen styles as assets. RETRO is the segmented art this project shipped with;
        /// ART is filled from Assets/Art/screen_v3 and marked complete only when every file is
        /// there - until then the loop keeps showing RETRO, and missing ART slots borrow the
        /// RETRO sprite so a half-delivered set can still be previewed by hand.
        /// </summary>
        static void CreateScreenStyles()
        {
            var retro = ResetToDefaults<ScreenStyle>(RetroStylePath);
            retro.id = "retro";
            retro.label = "RETRO";
            retro.monochrome = true;
            retro.bloom = true;
            retro.ghostsAllowed = true;
            retro.background = LoadSprite("Assets/Art/screen/screen_bg.png");
            retro.plank = null;
            retro.machineHead = LoadSprite("Assets/Art/sprites/machine_head.png");
            retro.baristaUp = LoadSprite("Assets/Art/sprites/barista_up.png");
            retro.baristaDown = LoadSprite("Assets/Art/sprites/barista_down.png");
            retro.baristaCatch = LoadSprite("Assets/Art/sprites/barista_catch.png");
            retro.baristaMiss = LoadSprite("Assets/Art/sprites/barista_miss.png");
            retro.baristaWipe = LoadSprite("Assets/Art/sprites/barista_wipe.png");
            retro.catA = LoadSprite("Assets/Art/sprites/cat_a.png");
            retro.catB = LoadSprite("Assets/Art/sprites/cat_b.png");
            retro.cup = LoadSprite("Assets/Art/sprites/cup.png");
            retro.cupsByOrder = new Sprite[0];
            retro.cupBroken = LoadSprite("Assets/Art/sprites/cup_broken.png");
            retro.stain = LoadSprite("Assets/Art/sprites/stain.png");
            retro.orderPanel = LoadSprite("Assets/Art/sprites/order_panel.png");
            retro.digitFont = Segment7Font;
            retro.letterFont = Segment14Font;
            retro.textFont = null;
            retro.complete = true;
            EditorUtility.SetDirty(retro);

            var art = ResetToDefaults<ScreenStyle>(ArtStylePath);
            art.id = "art";
            art.label = "ART";
            art.monochrome = false;
            art.bloom = false;
            art.ghostsAllowed = false;
            var found = new Dictionary<string, Sprite>();
            bool complete = true;
            foreach ((string slot, string file) in ArtSprites)
            {
                string path = $"{ScreenV3Dir}/{file}";
                var sprite = File.Exists(path) ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;
                if (sprite == null)
                    complete = false;
                found[slot] = sprite;
            }

            art.background = found["background"] ?? retro.background;
            art.plank = found["plank"];
            art.machineHead = found["machineHead"] ?? retro.machineHead;
            art.baristaUp = found["baristaUp"] ?? retro.baristaUp;
            art.baristaDown = found["baristaDown"] ?? retro.baristaDown;
            art.baristaCatch = found["baristaCatch"] ?? retro.baristaCatch;
            art.baristaMiss = found["baristaMiss"] ?? retro.baristaMiss;
            art.baristaWipe = found["baristaWipe"] ?? retro.baristaWipe;
            art.catA = found["catA"] ?? retro.catA;
            art.catB = found["catB"] ?? retro.catB;
            art.cup = found["cup0"] ?? retro.cup;
            art.cupsByOrder = found["cup0"] != null
                ? new[] { found["cup0"], found["cup1"], found["cup2"], found["cup3"] }
                : new Sprite[0];
            art.cupBroken = found["cupBroken"] ?? retro.cupBroken;
            art.stain = found["stain"] ?? retro.stain;
            art.orderPanel = found["orderPanel"] ?? retro.orderPanel;
            art.digitFont = null; // the painted HUD font is chosen once the first assets are in (GDD 5.2)
            art.letterFont = null;
            art.textFont = null;
            art.complete = complete;
            EditorUtility.SetDirty(art);

            if (!complete)
                Debug.Log($"[NightCafe] ScreenStyle ART incomplete: {ScreenV3Dir} is missing files, RETRO stays on screen.");
        }

        /// <summary>
        /// Counter planks for the painted style: one sprite per rail segment (start-bend,
        /// bend-end), stretched along the segment. Hidden until a style provides a plank sprite.
        /// </summary>
        static SpriteRenderer[] BuildPlanks(Transform screenRoot, LaneConfig laneConfig)
        {
            GameObject root = Child("Planks", screenRoot);
            var renderers = new List<SpriteRenderer>();
            foreach (LanePosition lane in System.Enum.GetValues(typeof(LanePosition)))
            {
                IReadOnlyList<Vector2> steps = laneConfig.GetSteps(lane);
                Vector2 start = steps[0], end = steps[steps.Count - 1];
                Vector2 bend = steps[steps.Count / 2];
                renderers.Add(Plank($"Plank_{lane}_A", root.transform, start, bend));
                renderers.Add(Plank($"Plank_{lane}_B", root.transform, bend, end));
            }

            root.SetActive(false);
            return renderers.ToArray();
        }

        static SpriteRenderer Plank(string name, Transform parent, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            var go = Child(name, parent, (from + to) * 0.5f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.drawMode = SpriteDrawMode.Sliced; // stretches along the rail without scaling the paint
            renderer.size = new Vector2(delta.magnitude + 0.25f, 0.5f);
            SetSorting(renderer, Core.SortingLayers.ScreenGlass, 1);
            return renderer;
        }

        /// <summary>Wires the applier to every styled renderer in the LCD scene.</summary>
        static ScreenStyleApplier BuildStyleApplier(Transform screenRoot, LaneConfig laneConfig, SpriteRenderer[] planks,
            PlayerPositionController barista, CatCrossingView cat, StainStripView stains, TimedSpriteFx[] brokenFx,
            OrderPanelView orderPanel)
        {
            var applier = screenRoot.gameObject.AddComponent<ScreenStyleApplier>();
            var heads = screenRoot.Find("MachineHeads").GetComponentsInChildren<SpriteRenderer>(true);
            var broken = new SpriteRenderer[brokenFx.Length];
            for (int i = 0; i < brokenFx.Length; i++)
                broken[i] = brokenFx[i].GetComponent<SpriteRenderer>();

            var digits = new List<TMP_Text>();
            var letters = new List<TMP_Text>();
            var body = new List<TMP_Text>();
            foreach (TMP_Text text in screenRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                switch (text.name)
                {
                    case "ScoreText":
                    case "ClockText":
                        digits.Add(text);
                        break;
                    case "BestText":
                    case "ModeText":
                        letters.Add(text);
                        break;
                    default:
                        body.Add(text);
                        break;
                }
            }

            var volume = Object.FindFirstObjectByType<Volume>();
            var plankRoot = screenRoot.Find("Planks");

            SetSerialized(applier, so =>
            {
                so.FindProperty("activeAmber").colorValue = ActiveAmber;
                so.FindProperty("brightAmber").colorValue = BrightAmber;
                so.FindProperty("inactiveAmber").colorValue = InactiveAmber;
                so.FindProperty("background").objectReferenceValue = screenRoot.Find("ScreenBG").GetComponent<SpriteRenderer>();
                so.FindProperty("plankRoot").objectReferenceValue = plankRoot != null ? plankRoot.gameObject : null;
                Fill(so.FindProperty("planks"), planks);
                Fill(so.FindProperty("machineHeads"), heads);
                so.FindProperty("lcdVolume").objectReferenceValue = volume;
                so.FindProperty("barista").objectReferenceValue = barista;
                so.FindProperty("baristaRenderer").objectReferenceValue = barista.GetComponent<SpriteRenderer>();
                so.FindProperty("cat").objectReferenceValue = cat;
                so.FindProperty("catRenderer").objectReferenceValue = cat.GetComponent<SpriteRenderer>();
                so.FindProperty("stains").objectReferenceValue = stains;
                Fill(so.FindProperty("brokenCups"), broken);
                so.FindProperty("orderPanel").objectReferenceValue = orderPanel;
                Fill(so.FindProperty("digitTexts"), digits.ToArray());
                Fill(so.FindProperty("letterTexts"), letters.ToArray());
                Fill(so.FindProperty("bodyTexts"), body.ToArray());
                so.FindProperty("baristaScale").floatValue = laneConfig.baristaScale;
                so.FindProperty("catScale").floatValue = laneConfig.catScale;
                so.FindProperty("stainScale").floatValue = laneConfig.stainScale;
                so.FindProperty("machineHeadScale").floatValue = laneConfig.machineHeadScale;
                so.FindProperty("brokenCupScale").floatValue = laneConfig.brokenCupScale;
                so.FindProperty("cupScale").floatValue = laneConfig.cupScale;
            });

            return applier;
        }

        static void Fill<T>(SerializedProperty array, T[] items) where T : Object
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
