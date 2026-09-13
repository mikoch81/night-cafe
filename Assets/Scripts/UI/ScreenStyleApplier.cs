using NightCafe.Config;
using NightCafe.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace NightCafe.UI
{
    /// <summary>
    /// Pushes a ScreenStyle into every renderer of the LCD scene: sprites, tints, fonts, the
    /// bloom volume and the counter planks. The scene generator wires the references; the game
    /// loop calls Apply when the player flips ART / RETRO on the title screen. Colours for the
    /// monochrome style come from the palette the setup serialised here.
    /// </summary>
    public sealed class ScreenStyleApplier : MonoBehaviour
    {
        [Header("Palette (RETRO tints)")]
        [SerializeField] Color activeAmber = new(1f, 0.788f, 0.4f);
        [SerializeField] Color brightAmber = new(1f, 0.824f, 0.478f);
        [SerializeField] Color inactiveAmber = new(0.227f, 0.173f, 0.094f);

        [Header("Scene")]
        [SerializeField] SpriteRenderer background;
        [SerializeField] GameObject plankRoot;
        [SerializeField] SpriteRenderer[] planks = new SpriteRenderer[0];
        [SerializeField] SpriteRenderer[] steps = new SpriteRenderer[0];
        [SerializeField] SpriteRenderer[] machineHeads = new SpriteRenderer[0];
        [SerializeField] SpriteRenderer scoreBoard;
        [SerializeField] Volume lcdVolume;

        [Header("Actors")]
        [SerializeField] PlayerPositionController barista;
        [SerializeField] SpriteRenderer baristaRenderer;
        [SerializeField] CatCrossingView cat;
        [SerializeField] SpriteRenderer catRenderer;
        [SerializeField] StainStripView stains;
        [SerializeField] SpriteRenderer[] brokenCups = new SpriteRenderer[0];
        [SerializeField] OrderPanelView orderPanel;

        [Header("Type")]
        [SerializeField] TMP_Text[] digitTexts = new TMP_Text[0];
        [SerializeField] TMP_Text[] letterTexts = new TMP_Text[0];
        [SerializeField] TMP_Text[] bodyTexts = new TMP_Text[0];

        [Header("Authored scales (LaneConfig), multiplied by the style")]
        [SerializeField] float baristaScale = 0.42f;
        [SerializeField] float catScale = 0.35f;
        [SerializeField] float stainScale = 0.35f;
        [SerializeField] float machineHeadScale = 0.5f;
        [SerializeField] float brokenCupScale = 0.5f;
        [SerializeField] float cupScale = 0.5f;

        TMP_FontAsset[] _digitDefaults, _letterDefaults, _bodyDefaults;
        Vector3[] _stepHomes;
        Vector3[] _plankHomes;

        public ScreenStyle Current { get; private set; }

        public void Apply(ScreenStyle style)
        {
            if (style == null)
                return;

            Current = style;
            RememberFonts();

            Color actor = style.monochrome ? activeAmber : Color.white;
            Color unlit = style.monochrome ? inactiveAmber : Color.clear;

            if (background != null && style.background != null)
                background.sprite = style.background;

            if (plankRoot != null)
                plankRoot.SetActive(style.plank != null || style.step != null);

            _stepHomes ??= System.Array.ConvertAll(steps, s => s != null ? s.transform.localPosition : Vector3.zero);
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] == null) continue;
                steps[i].sprite = style.step;
                steps[i].enabled = style.step != null;
                steps[i].transform.localPosition = _stepHomes[i] + (Vector3)style.baristaOffset;
                steps[i].transform.localScale = Vector3.one * (baristaScale * style.baristaScale);
            }
            _plankHomes ??= System.Array.ConvertAll(planks, p => p != null ? p.transform.localPosition : Vector3.zero);
            for (int i = 0; i < planks.Length; i++)
            {
                SpriteRenderer plank = planks[i];
                if (plank == null) continue;
                plank.sprite = style.plank;
                plank.enabled = style.plank != null;
                plank.transform.localPosition = _plankHomes[i] + (Vector3)style.plankOffset;
                // Sliced draw mode: the setup sized the plank along the rail segment; only the
                // thickness follows the style (the end caps keep their pixels).
                plank.size = new Vector2(plank.size.x, style.plankHeight);
            }

            if (scoreBoard != null)
            {
                scoreBoard.sprite = style.scoreBoard;
                scoreBoard.enabled = style.scoreBoard != null;
            }

            foreach (SpriteRenderer head in machineHeads)
            {
                if (head == null) continue;
                if (style.machineHead != null) head.sprite = style.machineHead;
                head.color = style.monochrome ? inactiveAmber : Color.white;
                Vector3 s = head.transform.localScale;
                float magnitude = machineHeadScale * style.machineHeadScale;
                head.transform.localScale = new Vector3(Mathf.Sign(s.x) * magnitude, magnitude, 1f);
            }

            if (barista != null)
            {
                barista.SetPoses(style.baristaUp, style.baristaDown, style.baristaCatch, style.baristaMiss, style.baristaWipe);
                barista.SetScale(baristaScale * style.baristaScale);
                barista.SetOffset(style.baristaOffset);
                barista.SetMoveSeconds(style.moveSeconds);
            }
            if (baristaRenderer != null)
                baristaRenderer.color = actor;

            if (cat != null)
            {
                cat.SetFrames(style.catA, style.catB);
                cat.SetMotion(style.catBob, style.catTilt);
            }
            if (catRenderer != null)
            {
                catRenderer.color = actor;
                catRenderer.transform.localScale = Vector3.one * (catScale * style.catScale);
            }

            if (stains != null)
                stains.SetStyle(style.stain, actor, unlit, stainScale * style.stainScale);

            foreach (SpriteRenderer broken in brokenCups)
            {
                if (broken == null) continue;
                if (style.cupBroken != null) broken.sprite = style.cupBroken;
                broken.color = actor;
                broken.transform.localScale = Vector3.one * (brokenCupScale * style.brokenCupScale);
            }

            if (orderPanel != null)
                orderPanel.SetStyle(style.orderPanel, style.HasPaintedCups ? style.cupsByOrder : null, style.orderCupScale);

            CupSkin.Set(style, style.monochrome ? brightAmber : Color.white, cupScale * style.cupScale);

            ApplyFonts(digitTexts, _digitDefaults, style.digitFont);
            ApplyFonts(letterTexts, _letterDefaults, style.letterFont);
            ApplyFonts(bodyTexts, _bodyDefaults, style.textFont);

            if (lcdVolume != null)
                lcdVolume.weight = style.bloom ? 1f : 0f;
        }

        void RememberFonts()
        {
            _digitDefaults ??= Fonts(digitTexts);
            _letterDefaults ??= Fonts(letterTexts);
            _bodyDefaults ??= Fonts(bodyTexts);
        }

        static TMP_FontAsset[] Fonts(TMP_Text[] texts)
        {
            var result = new TMP_FontAsset[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                result[i] = texts[i] != null ? texts[i].font : null;
            return result;
        }

        static void ApplyFonts(TMP_Text[] texts, TMP_FontAsset[] defaults, TMP_FontAsset font)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null) continue;
                TMP_FontAsset target = font != null ? font : defaults[i];
                if (target != null && texts[i].font != target)
                    texts[i].font = target;
            }
        }
    }
}
