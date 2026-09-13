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
        static (HudView hud, TitleToggleView toggles, ClockWidget clock) BuildHud(Transform screenRoot, TMP_FontAsset monoFont)
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
            GameObject titlePanel = Child("TitlePanel", root);
            TMP_Text clock = WorldText("ClockText", titlePanel.transform, new Vector2(0f, 1.30f),
                "23:41", 11f, digitFont, BrightAmber, 0, new Vector2(8f, 1.6f));

            WorldText("TitleText", titlePanel.transform, new Vector2(0f, -0.30f),
                "NIGHT CAFÉ\nTAP TO START", 7f, monoFont, ActiveAmber, 0, new Vector2(11f, 2.6f));

            TMP_Text brewTag = WorldText("BrewTag", titlePanel.transform, new Vector2(3.55f, 1.62f),
                "BREW", 3.2f, monoFont, ActiveAmber, 0, new Vector2(2f, 0.6f));
            brewTag.gameObject.SetActive(false);

            var clockWidget = titlePanel.AddComponent<ClockWidget>();
            SetSerialized(clockWidget, so =>
            {
                so.FindProperty("label").objectReferenceValue = clock;
                so.FindProperty("brewTag").objectReferenceValue = brewTag;
            });

            GameObject togglesGo = Child("Toggles", titlePanel.transform, new Vector2(0f, -2.30f));
            // Five toggles across the 12.7-wide LCD: sound, haptics, ghosts, shell skin, screen style.
            const float toggleStep = 2.45f;
            const float toggleSize = 5.2f;
            TMP_Text soundLabel = WorldText("SoundToggle", togglesGo.transform, new Vector2(-2f * toggleStep, 0f),
                "♪ ON", toggleSize, monoFont, ActiveAmber, 0, new Vector2(2.4f, 1f));
            TMP_Text hapticsLabel = WorldText("HapticsToggle", togglesGo.transform, new Vector2(-toggleStep, 0f),
                "~ ON", toggleSize, monoFont, ActiveAmber, 0, new Vector2(2.4f, 1f));
            TMP_Text ghostsLabel = WorldText("GhostsToggle", togglesGo.transform, new Vector2(0f, 0f),
                "░ OFF", toggleSize, monoFont, InactiveAmber, 0, new Vector2(2.4f, 1f));
            TMP_Text skinLabel = WorldText("SkinToggle", togglesGo.transform, new Vector2(toggleStep, 0f),
                "WALNUT", toggleSize, monoFont, ActiveAmber, 0, new Vector2(2.4f, 1f));
            TMP_Text screenLabel = WorldText("ScreenToggle", togglesGo.transform, new Vector2(2f * toggleStep, 0f),
                "RETRO", toggleSize, monoFont, ActiveAmber, 0, new Vector2(2.4f, 1f));

            var toggles = togglesGo.AddComponent<TitleToggleView>();
            SetSerialized(toggles, so =>
            {
                so.FindProperty("soundLabel").objectReferenceValue = soundLabel;
                so.FindProperty("hapticsLabel").objectReferenceValue = hapticsLabel;
                so.FindProperty("ghostsLabel").objectReferenceValue = ghostsLabel;
                so.FindProperty("skinLabel").objectReferenceValue = skinLabel;
                so.FindProperty("screenLabel").objectReferenceValue = screenLabel;
                so.FindProperty("hitSize").vector2Value = new Vector2(2.3f, 0.9f);
                so.FindProperty("onColor").colorValue = ActiveAmber;
                so.FindProperty("offColor").colorValue = InactiveAmber;
            });

            // --- Attract demo ---------------------------------------------------
            GameObject demoPanel = Child("DemoPanel", root);
            TMP_Text demoText = WorldText("DemoText", demoPanel.transform, new Vector2(0f, 2.55f),
                "DEMO - TAP TO START", 5f, monoFont, ActiveAmber, 0, new Vector2(9f, 0.8f));
            demoPanel.SetActive(false);

            // --- Game over -----------------------------------------------------
            GameObject gameOverPanel = Child("GameOverPanel", root);
            TMP_Text gameOverText = WorldText("GameOverText", gameOverPanel.transform, Vector2.zero,
                "END OF SHIFT\n000\nBEST 000\nTAP TO RESTART", 7f, monoFont, ActiveAmber, 0, new Vector2(11f, 4.4f));
            gameOverPanel.SetActive(false);

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
                so.FindProperty("gameOverText").objectReferenceValue = gameOverText;
                so.FindProperty("fpsText").objectReferenceValue = fps;
            });

            return (hud, toggles, clockWidget);
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
