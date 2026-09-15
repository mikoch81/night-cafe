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
            ("step", "step.png"), ("scoreBoard", "scoreboard.png"),
        };

        /// <summary>
        /// Title and end-of-shift dressing (prompts 18-19). Optional: the style is complete
        /// without them, an absent prop just leaves its lettering on the painting.
        /// </summary>
        public static readonly (string slot, string file)[] ArtTitleSprites =
        {
            ("titleSign", "title_sign.png"), ("clockFace", "clock_face.png"),
            ("cardA", "card_a.png"), ("cardB", "card_b.png"), ("resultCard", "result_card.png"),
            ("catAsleepA", "sable_sleep_a.png"), ("catAsleepB", "sable_sleep_b.png"),
        };

        const string HandBoldFontPath = FontDir + "/CabinSketch-Bold.ttf";
        const string HandBoldFontAssetPath = FontDir + "/CabinSketch-Bold SDF.asset";
        const string HandFontPath = FontDir + "/PatrickHand-Regular.ttf";
        const string HandFontAssetPath = FontDir + "/PatrickHand-Regular SDF.asset";

        /// <summary>
        /// The two screen styles as assets. RETRO is the segmented art this project shipped with;
        /// ART is filled from Assets/Art/screen_v3 and marked complete only when every file is
        /// there - until then the loop keeps showing RETRO, and missing ART slots borrow the
        /// RETRO sprite so a half-delivered set can still be previewed by hand.
        /// </summary>
        static void CreateScreenStyles(AudioConfig artSounds, LaneConfig laneConfig)
        {
            var retro = ResetToDefaults<ScreenStyle>(RetroStylePath);
            retro.sounds = null;                         // the scene's base set: the chiptune from gen_audio.py
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
            // Both idle slots use the tray-down pose: the painted Miro reaches the upper shelf from
            // the footstool, not by raising the tray (which would put it well above the rail end).
            art.baristaUp = found["baristaDown"] ?? retro.baristaUp;
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
            art.step = found["step"];
            art.scoreBoard = found["scoreBoard"];
            art.moveSeconds = 0.12f;
            art.catBob = 0.08f;
            art.catTilt = 4f;
            // The plank is one straight board from K1 to K5; the lane's bent steps (drawn for
            // the RETRO rail) would float a cup 0.2 above it mid-way. Cups slide along the
            // board instead, rocking as they go.
            art.cupsRideStraightRail = true;
            art.cupWobble = 3f;
            art.cupWobbleHz = 7f;
            art.cupBob = 0.03f;
            // Chalk-menu lettering (OFL): Cabin Sketch for the counter and headings, Patrick Hand for the rest.
            TMP_FontAsset handBold = EnsureSegmentFont(HandBoldFontPath, HandBoldFontAssetPath, "CabinSketch-Bold", HudCharset);
            TMP_FontAsset hand = EnsureSegmentFont(HandFontPath, HandFontAssetPath, "PatrickHand-Regular", HudCharset);
            art.digitFont = handBold;
            art.letterFont = handBold;
            art.textFont = hand;
            // Sizes on top of LaneConfig: Miro about two units tall, the cat a little over one,
            // the machine 1.5 with its spout on the rail start; the tray then sits a touch above
            // the rail end in the up pose and a touch below it in the down pose.
            art.baristaScale = 0.96f;                    // ~2.5 units tall; the tray-down tray meets the rail end
            art.baristaOffset = new Vector2(0f, -0.4f);
            art.catScale = 2.0f;
            art.cupScale = 0.7f;                         // the painted cups read big on the board (Michal, round 4)
            art.brokenCupScale = 0.7f;                   // the shards match the cup; the order-panel cup keeps its own scale
            art.machineHeadScale = 0.34f;
            art.plankHeight = 0.6f;
            art.plankOffset = new Vector2(0f, -0.27f);   // cups rest on the top face
            art.orderCupScale = new Vector2(0.6f, 0.6f);
            // Ceramic, rain and a real lo-fi record for the diorama; the chiptune stays with RETRO.
            art.sounds = artSounds;
            art.complete = complete;
            ApplyArtTitleDressing(art, laneConfig);
            EditorUtility.SetDirty(art);

            if (!complete)
                Debug.Log($"[NightCafe] ScreenStyle ART incomplete: {ScreenV3Dir} is missing files, RETRO stays on screen.");
        }

        /// <summary>
        /// The painted title: a chalk sign and the clock's dial in the middle row, the settings
        /// pinned as notes on the left wall and Miro wiping the counter on the right; at the end
        /// of the shift the lights go down, the receipt lies on the counter and Sablé sleeps by
        /// it. Every prop is optional - a missing file just leaves its lettering on the painting.
        /// </summary>
        static void ApplyArtTitleDressing(ScreenStyle art, LaneConfig laneConfig)
        {
            var found = new Dictionary<string, Sprite>();
            foreach ((string slot, string file) in ArtTitleSprites)
            {
                string path = $"{ScreenV3Dir}/{file}";
                found[slot] = File.Exists(path) ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;
            }

            art.titleSign = found["titleSign"];
            art.titleSignSize = new Vector2(7.2f, 2.7f);
            art.clockFace = found["clockFace"];
            art.clockFaceWidth = 1.9f;
            art.toggleCards = found["cardA"] != null
                ? (found["cardB"] != null ? new[] { found["cardA"], found["cardB"] } : new[] { found["cardA"] })
                : new Sprite[0];
            art.toggleCardWidth = 1.9f;                  // the notes are portrait (587x723): 1.9 wide is 2.3 tall
            art.resultCard = found["resultCard"];
            art.resultCardSize = new Vector2(5.4f, 4.8f);
            art.catAsleepA = found["catAsleepA"];
            art.catAsleepB = found["catAsleepB"] ?? found["catAsleepA"];
            art.catAsleepX = 4.3f;

            art.titleBaristaWipes = true;
            art.titleBaristaPosition = new Vector2(3.6f, laneConfig.barLineY);
            art.titleBaristaFacesLeft = true;
            art.titleWipePeriod = 0.7f;

            art.signInk = new Color(0.95f, 0.91f, 0.82f);      // chalk
            art.cardInk = new Color(0.23f, 0.16f, 0.11f);      // ink on cream card
            art.cardInkFaded = new Color(0.63f, 0.55f, 0.45f);
            art.accentInk = new Color(0.73f, 0.27f, 0.18f);    // the red stamp
            art.gameOverDim = 0.72f;                           // lights down for the night

            // Layout: sign and clock share the row under the score; the notes stagger down the
            // left wall in two overlapping rows (sound, haptics, ghosts / skin, screen), clear of
            // Miro wiping on the right. The lettering sits low on each note, under its pin.
            art.titlePosition = new Vector2(-1.4f, 1.45f);
            art.clockPosition = new Vector2(4.4f, 1.45f);
            art.brewTagOffset = new Vector2(0f, -1.2f);
            art.togglesPosition = Vector2.zero;
            art.toggleOffsets = new[]
            {
                new Vector2(-4.9f, -1.05f), new Vector2(-2.9f, -1.05f), new Vector2(-0.9f, -1.05f),
                new Vector2(-3.9f, -3.05f), new Vector2(-1.9f, -3.05f),
            };
            art.toggleLabelOffset = new Vector2(0f, -0.25f);
            art.resultPosition = new Vector2(0f, -0.3f);
        }

        /// <summary>
        /// Wall shelves for the painted style: one plank per rail from its start to its catch
        /// point (the rail's slight bend is within the plank's thickness). Hidden until a style
        /// provides a plank sprite.
        /// </summary>
        static SpriteRenderer[] BuildPlanks(Transform screenRoot, LaneConfig laneConfig)
        {
            GameObject root = Child("Props", screenRoot);
            var renderers = new List<SpriteRenderer>();
            foreach (LanePosition lane in System.Enum.GetValues(typeof(LanePosition)))
            {
                IReadOnlyList<Vector2> steps = laneConfig.GetSteps(lane);
                renderers.Add(Plank($"Plank_{lane}", root.transform, steps[0], steps[steps.Count - 1]));
            }

            root.SetActive(false);
            return renderers.ToArray();
        }

        /// <summary>The board behind the score (ART only): Segments layer, under the HUD text.</summary>
        static SpriteRenderer BuildScoreBoard(Transform screenRoot)
        {
            var go = Child("ScoreBoard", screenRoot.Find("Props"), new Vector2(0f, 3.55f));
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.enabled = false;
            SetSorting(renderer, Core.SortingLayers.Segments, 40);
            return renderer;
        }

        /// <summary>The footstools under the two upper barista slots (ART only; sprite set by the applier).</summary>
        static SpriteRenderer[] BuildSteps(Transform screenRoot, LaneConfig laneConfig)
        {
            Transform root = screenRoot.Find("Props");
            var result = new SpriteRenderer[2];
            LanePosition[] upper = { LanePosition.LeftUp, LanePosition.RightUp };
            for (int i = 0; i < upper.Length; i++)
            {
                var go = Child($"Step_{upper[i]}", root, laneConfig.GetBaristaSlot(upper[i]), laneConfig.baristaScale);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.enabled = false;
                SetSorting(renderer, Core.SortingLayers.Segments, 29); // just under the barista (30)
                result[i] = renderer;
            }
            return result;
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
        static ScreenStyleApplier BuildStyleApplier(Transform screenRoot, LaneConfig laneConfig, SpriteRenderer[] planks, SpriteRenderer[] steps,
            PlayerPositionController barista, CatCrossingView cat, StainStripView stains, TimedSpriteFx[] brokenFx,
            OrderPanelView orderPanel, HudView hud, TitleToggleView toggles, ClockWidget clock, TitleProps titleProps)
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
                    case "GameOverScore":
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
            var plankRoot = screenRoot.Find("Props");
            SpriteRenderer scoreBoard = BuildScoreBoard(screenRoot);

            SetSerialized(applier, so =>
            {
                so.FindProperty("activeAmber").colorValue = ActiveAmber;
                so.FindProperty("brightAmber").colorValue = BrightAmber;
                so.FindProperty("inactiveAmber").colorValue = InactiveAmber;
                so.FindProperty("background").objectReferenceValue = screenRoot.Find("ScreenBG").GetComponent<SpriteRenderer>();
                so.FindProperty("plankRoot").objectReferenceValue = plankRoot != null ? plankRoot.gameObject : null;
                Fill(so.FindProperty("planks"), planks);
                Fill(so.FindProperty("steps"), steps);
                so.FindProperty("scoreBoard").objectReferenceValue = scoreBoard;
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
                so.FindProperty("hud").objectReferenceValue = hud;
                so.FindProperty("toggles").objectReferenceValue = toggles;
                so.FindProperty("clock").objectReferenceValue = clock;
                so.FindProperty("titleGroup").objectReferenceValue = titleProps.titleGroup;
                so.FindProperty("clockGroup").objectReferenceValue = titleProps.clockGroup;
                so.FindProperty("togglesGroup").objectReferenceValue = titleProps.togglesGroup;
                so.FindProperty("resultGroup").objectReferenceValue = titleProps.resultGroup;
                so.FindProperty("brewTag").objectReferenceValue = titleProps.brewTag;
                so.FindProperty("titleSign").objectReferenceValue = titleProps.titleSign;
                Fill(so.FindProperty("toggleLabels"), titleProps.toggleLabels);
                Fill(so.FindProperty("toggleCards"), titleProps.toggleCards);
                so.FindProperty("demoBacking").objectReferenceValue = titleProps.demoBacking;
                so.FindProperty("resultCard").objectReferenceValue = titleProps.resultCard;
                Fill(so.FindProperty("signTexts"), titleProps.signTexts);
                Fill(so.FindProperty("cardTexts"), titleProps.cardTexts);
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
