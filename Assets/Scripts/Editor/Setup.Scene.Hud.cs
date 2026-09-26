using NightCafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightCafe.EditorTools
{
    public static partial class NightCafeSetup
    {
        /// <summary>
        /// The in-LCD HUD is world-space TextMeshPro parented to the screen, so it scales and
        /// sorts with the glass (GDD 5.1 HUD_TMP). Only the debug FPS readout stays on an
        /// overlay canvas, where it belongs on top of everything.
        /// </summary>
        static (HudView hud, TitleToggleView toggles, ClockWidget clock, TitleProps props) BuildHud(Transform screenRoot, TMP_FontAsset monoFont)
        {
            Transform root = Child("ScreenHud", screenRoot).transform;
            TMP_FontAsset digitFont = Segment7Font != null ? Segment7Font : monoFont;
            TMP_FontAsset letterFont = Segment14Font != null ? Segment14Font : monoFont;

            TMP_Text score = WorldText("ScoreText", root, new Vector2(0f, 3.55f), "000", 14f, digitFont,
                ActiveAmber, 0, new Vector2(8f, 1.8f));

            TMP_Text mode = WorldText("ModeText", root, new Vector2(4.60f, 3.55f), "A", 8f, letterFont,
                ActiveAmber, 0, new Vector2(2f, 1.2f));

            TMP_Text best = WorldText("BestText", root, new Vector2(-4.30f, 3.55f), "BEST 000", 4.6f, letterFont,
                ActiveAmber, 0, new Vector2(3.9f, 1.2f));
            best.wordSpacing = 12f; // DSEG's space is a bare segment gap; open it up between BEST and the digits

            // --- Title ---------------------------------------------------------
            // Each block sits in its own group so a screen style can lay the title out
            // differently (ScreenStyle.titlePosition / clockPosition / togglesY) and hang a
            // painted prop behind the lettering (Hud layer, under the text).
            GameObject titlePanel = Child("TitlePanel", root);

            GameObject clockGroup = Child("ClockGroup", titlePanel.transform, new Vector2(0f, 1.30f));
            SpriteRenderer clockFace = Prop("ClockFace", clockGroup.transform);
            (Transform hourHand, SpriteRenderer hourInk) = ClockHand("HourHand", clockGroup.transform, 0.08f, 0.50f);
            (Transform minuteHand, SpriteRenderer minuteInk) = ClockHand("MinuteHand", clockGroup.transform, 0.06f, 0.70f);
            TMP_Text clock = WorldText("ClockText", clockGroup.transform, Vector2.zero,
                "23:41", 11f, digitFont, BrightAmber, 0, new Vector2(8f, 1.6f));
            TMP_Text brewTag = WorldText("BrewTag", clockGroup.transform, new Vector2(3.55f, 0.32f),
                "BREW", 3.2f, monoFont, ActiveAmber, 0, new Vector2(2f, 0.6f));
            brewTag.gameObject.SetActive(false);

            var clockWidget = titlePanel.AddComponent<ClockWidget>();
            SetSerialized(clockWidget, so =>
            {
                so.FindProperty("label").objectReferenceValue = clock;
                so.FindProperty("brewTag").objectReferenceValue = brewTag;
                so.FindProperty("face").objectReferenceValue = clockFace;
                so.FindProperty("hourHand").objectReferenceValue = hourHand;
                so.FindProperty("minuteHand").objectReferenceValue = minuteHand;
                Fill(so.FindProperty("handInks"), new[] { hourInk, minuteInk });
            });

            GameObject titleGroup = Child("TitleGroup", titlePanel.transform, new Vector2(0f, -0.30f));
            SpriteRenderer titleSign = Prop("TitleSign", titleGroup.transform);
            TMP_Text titleText = WorldText("TitleText", titleGroup.transform, Vector2.zero,
                "NIGHT CAFÉ\nTAP TO START", 7f, monoFont, ActiveAmber, 0, new Vector2(11f, 2.6f));

            GameObject togglesGo = Child("Toggles", titlePanel.transform, new Vector2(0f, -2.30f));
            // Four toggles across the 12.7-wide LCD: music, sound effects, haptics, shell skin.
            const float toggleStep = 2.45f;
            const float toggleSize = 5.2f;
            var toggleCards = new SpriteRenderer[4];
            for (int i = 0; i < toggleCards.Length; i++)
                toggleCards[i] = Prop($"Card_{i}", togglesGo.transform, new Vector2((i - 1.5f) * toggleStep, 0f));
            TMP_Text musicLabel = WorldText("MusicToggle", togglesGo.transform, new Vector2(-1.5f * toggleStep, 0f),
                "♪ ON", toggleSize, monoFont, ActiveAmber, 0, new Vector2(2.4f, 1f));
            TMP_Text soundLabel = WorldText("SoundToggle", togglesGo.transform, new Vector2(-0.5f * toggleStep, 0f),
                "SFX ON", toggleSize, monoFont, ActiveAmber, 0, new Vector2(2.4f, 1f));
            TMP_Text hapticsLabel = WorldText("HapticsToggle", togglesGo.transform, new Vector2(0.5f * toggleStep, 0f),
                "~ ON", toggleSize, monoFont, ActiveAmber, 0, new Vector2(2.4f, 1f));
            TMP_Text skinLabel = WorldText("SkinToggle", togglesGo.transform, new Vector2(1.5f * toggleStep, 0f),
                "WALNUT", toggleSize, monoFont, ActiveAmber, 0, new Vector2(2.4f, 1f));

            var toggles = togglesGo.AddComponent<TitleToggleView>();
            SetSerialized(toggles, so =>
            {
                so.FindProperty("musicLabel").objectReferenceValue = musicLabel;
                so.FindProperty("soundLabel").objectReferenceValue = soundLabel;
                so.FindProperty("hapticsLabel").objectReferenceValue = hapticsLabel;
                so.FindProperty("skinLabel").objectReferenceValue = skinLabel;
                so.FindProperty("hitSize").vector2Value = new Vector2(2.3f, 0.9f);
                so.FindProperty("onColor").colorValue = ActiveAmber;
                so.FindProperty("offColor").colorValue = InactiveAmber;
            });

            // --- Attract demo ---------------------------------------------------
            GameObject demoPanel = Child("DemoPanel", root);
            // A strip of dark glass under the prompt for the painted style (the applier enables it).
            var backingGo = Child("DemoBacking", demoPanel.transform, new Vector2(0f, 2.55f));
            backingGo.transform.localScale = new Vector3(9.4f, 1.0f, 1f);
            var demoBacking = backingGo.AddComponent<SpriteRenderer>();
            demoBacking.sprite = WhitePixelSprite();
            demoBacking.color = new Color(GlassBlack.r, GlassBlack.g, GlassBlack.b, 0.55f);
            demoBacking.enabled = false;
            SetSorting(demoBacking, Core.SortingLayers.Hud, PropOrder);
            TMP_Text demoText = WorldText("DemoText", demoPanel.transform, new Vector2(0f, 2.55f),
                "DEMO - TAP TO START", 5f, monoFont, ActiveAmber, 0, new Vector2(9f, 0.8f));
            demoPanel.SetActive(false);

            // --- Game over -----------------------------------------------------
            // Four lines in their own type: heading, the counter, the record, the footer.
            GameObject gameOverPanel = Child("GameOverPanel", root);
            GameObject resultGroup = Child("ResultGroup", gameOverPanel.transform);
            SpriteRenderer resultCard = Prop("ResultCard", resultGroup.transform);
            TMP_Text gameOverHeader = WorldText("GameOverHeader", resultGroup.transform, new Vector2(0f, 1.55f),
                "END OF SHIFT", 5.5f, monoFont, ActiveAmber, 0, new Vector2(8f, 1f));
            TMP_Text gameOverScore = WorldText("GameOverScore", resultGroup.transform, new Vector2(0f, 0.45f),
                "000", 12f, digitFont, ActiveAmber, 0, new Vector2(8f, 1.6f));
            TMP_Text gameOverRecord = WorldText("GameOverRecord", resultGroup.transform, new Vector2(0f, -0.65f),
                "BEST 000", 5f, monoFont, ActiveAmber, 0, new Vector2(8f, 0.9f));
            TMP_Text gameOverFooter = WorldText("GameOverFooter", resultGroup.transform, new Vector2(0f, -1.6f),
                "TAP TO RESTART", 4.2f, monoFont, ActiveAmber, 0, new Vector2(8f, 1.4f));
            gameOverPanel.SetActive(false);

            // --- Menu (review 2026-09-26) ---------------------------------------
            (PauseMenuView menu, SpriteRenderer menuCard, TMP_Text[] menuTexts) = BuildMenu(root, monoFont);

            // --- Debug overlay --------------------------------------------------
            TMP_Text fps = BuildDebugCanvas(monoFont);

            var hud = root.gameObject.AddComponent<HudView>();
            SetSerialized(hud, so =>
            {
                so.FindProperty("scoreText").objectReferenceValue = score;
                so.FindProperty("bestText").objectReferenceValue = best;
                so.FindProperty("modeText").objectReferenceValue = mode;
                so.FindProperty("titlePanel").objectReferenceValue = titlePanel;
                so.FindProperty("demoPanel").objectReferenceValue = demoPanel;
                so.FindProperty("demoText").objectReferenceValue = demoText;
                so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
                so.FindProperty("gameOverHeader").objectReferenceValue = gameOverHeader;
                so.FindProperty("gameOverScore").objectReferenceValue = gameOverScore;
                so.FindProperty("gameOverRecord").objectReferenceValue = gameOverRecord;
                so.FindProperty("gameOverFooter").objectReferenceValue = gameOverFooter;
                so.FindProperty("fpsText").objectReferenceValue = fps;
            });

            return (hud, toggles, clockWidget, new TitleProps
            {
                menu = menu,
                menuCard = menuCard,
                menuTexts = menuTexts,
                titleGroup = titleGroup.transform,
                clockGroup = clockGroup.transform,
                togglesGroup = togglesGo.transform,
                resultGroup = resultGroup.transform,
                brewTag = brewTag.transform,
                titleSign = titleSign,
                toggleLabels = new[] { musicLabel, soundLabel, hapticsLabel, skinLabel },
                toggleCards = toggleCards,
                demoBacking = demoBacking,
                resultCard = resultCard,
                signTexts = new[] { titleText },
                cardTexts = new[] { gameOverHeader, gameOverScore, gameOverFooter },
            });
        }

        /// <summary>Sorting order of a painted prop on the Hud layer: under the lettering (0), hands in between.</summary>
        const int PropOrder = -10;
        const int HandOrder = -8;

        /// <summary>Everything the style applier needs to dress the title and the end of shift.</summary>
        internal sealed class TitleProps
        {
            public Transform titleGroup, clockGroup, togglesGroup, resultGroup, brewTag;
            public SpriteRenderer titleSign, demoBacking, resultCard, menuCard;
            public SpriteRenderer[] toggleCards;
            public TMP_Text[] toggleLabels, signTexts, cardTexts, menuTexts;
            public PauseMenuView menu;
        }

        // The menu draws over everything else in the LCD, the HUD digits included.
        const int MenuDimOrder = 40;
        const int MenuCardOrder = 45;
        const int MenuTextOrder = 50;

        /// <summary>
        /// The MENU card: a dark veil over the whole screen, a paper card (the receipt sprite,
        /// 9-sliced by the style applier) and the rows. Two pages share the header: the rows,
        /// and the local top 10 with a BACK row.
        /// </summary>
        static (PauseMenuView view, SpriteRenderer card, TMP_Text[] texts) BuildMenu(Transform hudRoot, TMP_FontAsset font)
        {
            GameObject host = Child("Menu", hudRoot);
            GameObject panel = Child("MenuPanel", host.transform);

            var veilGo = Child("MenuVeil", panel.transform);
            veilGo.transform.localScale = new Vector3(12.72f, 8.92f, 1f);
            var veil = veilGo.AddComponent<SpriteRenderer>();
            veil.sprite = WhitePixelSprite();
            veil.color = new Color(GlassBlack.r, GlassBlack.g, GlassBlack.b, 0.6f);
            SetSorting(veil, Core.SortingLayers.Hud, MenuDimOrder);

            var cardGo = Child("MenuCard", panel.transform);
            var card = cardGo.AddComponent<SpriteRenderer>();
            card.enabled = false; // the applier fits the paper
            SetSorting(card, Core.SortingLayers.Hud, MenuCardOrder);

            var rowSize = new Vector2(6f, 0.8f);
            const float rowFont = 5.2f;
            TMP_Text Row(string name, Transform parent, float y, string text, float size = rowFont) =>
                WorldText(name, parent, new Vector2(0f, y), text, size, font, ActiveAmber, MenuTextOrder, rowSize);

            TMP_Text header = Row("MenuHeader", panel.transform, 3.3f, "PAUSED", 6f);

            GameObject main = Child("MainPage", panel.transform);
            TMP_Text resume = Row("ResumeRow", main.transform, 2.3f, "RESUME");
            TMP_Text music = Row("MusicRow", main.transform, 1.4f, "MUSIC  ON");
            TMP_Text sound = Row("SoundRow", main.transform, 0.5f, "SOUND  ON");
            TMP_Text mode = Row("ModeRow", main.transform, -0.4f, "MODE  A");
            TMP_Text scores = Row("ScoresRow", main.transform, -1.3f, "TOP 10");
            TMP_Text endShift = Row("EndShiftRow", main.transform, -2.2f, "END SHIFT");
            TMP_Text quit = Row("QuitRow", main.transform, -3.1f, "QUIT GAME");

            GameObject scoresPage = Child("ScoresPage", panel.transform);
            TMP_Text list = WorldText("ScoresText", scoresPage.transform, new Vector2(0f, -0.1f), "NO SHIFTS YET", 3.9f,
                font, ActiveAmber, MenuTextOrder, new Vector2(6f, 5.8f));
            list.alignment = TextAlignmentOptions.Top;
            TMP_Text back = Row("BackRow", scoresPage.transform, -3.4f, "BACK");
            scoresPage.SetActive(false);
            panel.SetActive(false);

            var view = host.AddComponent<PauseMenuView>();
            SetSerialized(view, so =>
            {
                so.FindProperty("root").objectReferenceValue = panel;
                so.FindProperty("header").objectReferenceValue = header;
                so.FindProperty("resumeRow").objectReferenceValue = resume;
                so.FindProperty("musicRow").objectReferenceValue = music;
                so.FindProperty("soundRow").objectReferenceValue = sound;
                so.FindProperty("modeRow").objectReferenceValue = mode;
                so.FindProperty("scoresRow").objectReferenceValue = scores;
                so.FindProperty("endShiftRow").objectReferenceValue = endShift;
                so.FindProperty("quitRow").objectReferenceValue = quit;
                so.FindProperty("mainPage").objectReferenceValue = main;
                so.FindProperty("scoresPage").objectReferenceValue = scoresPage;
                so.FindProperty("scoresText").objectReferenceValue = list;
                so.FindProperty("backRow").objectReferenceValue = back;
            });

            return (view, card, new[] { header, resume, music, sound, mode, scores, endShift, quit, list, back });
        }

        /// <summary>A hidden sprite slot the applier fills from the style (Hud layer, under the text).</summary>
        static SpriteRenderer Prop(string name, Transform parent, Vector2 localPosition = default)
        {
            var go = Child(name, parent, localPosition);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.enabled = false;
            SetSorting(renderer, Core.SortingLayers.Hud, PropOrder);
            return renderer;
        }

        /// <summary>
        /// A clock hand: a pivot at the dial's centre that the widget rotates, with a thin quad
        /// hanging up from it (length and width in LCD units). Hidden until a dial arrives.
        /// </summary>
        static (Transform pivot, SpriteRenderer ink) ClockHand(string name, Transform parent, float width, float length)
        {
            GameObject pivot = Child(name, parent);
            var bar = Child("Ink", pivot.transform, new Vector2(0f, length * 0.5f - 0.02f));
            bar.transform.localScale = new Vector3(width, length, 1f);
            var renderer = bar.AddComponent<SpriteRenderer>();
            renderer.sprite = WhitePixelSprite();
            SetSorting(renderer, Core.SortingLayers.Hud, HandOrder);
            pivot.SetActive(false);
            return (pivot.transform, renderer);
        }

        static TMP_Text BuildDebugCanvas(TMP_FontAsset monoFont)
        {
            var canvasGo = new GameObject("DebugCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var go = new GameObject("FpsText", typeof(RectTransform));
            go.transform.SetParent(canvasGo.transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-140f, 50f);
            rect.sizeDelta = new Vector2(240f, 60f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = "-- fps";
            text.fontSize = 32f;
            text.color = ActiveAmber;
            text.alignment = TextAlignmentOptions.Center;
            if (monoFont != null)
                text.font = monoFont;

            return text;
        }

        static TMP_Text WorldText(string name, Transform parent, Vector2 localPosition, string content,
            float fontSize, TMP_FontAsset font, Color color, int sortingOrder, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            var text = go.AddComponent<TextMeshPro>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = false;
            if (font != null)
                text.font = font;

            var rect = text.rectTransform;
            rect.sizeDelta = size;
            rect.pivot = new Vector2(0.5f, 0.5f);

            // World-space TMP draws through a MeshRenderer, so the sorting layer goes there.
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
                SetSorting(renderer, Core.SortingLayers.Hud, sortingOrder);

            return text;
        }
    }
}
