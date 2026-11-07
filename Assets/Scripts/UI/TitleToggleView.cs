using NightCafe.Core;
using NightCafe.Services;
using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>What a tap on the title cards was.</summary>
    public enum TitleTap { None, Toggle, Online, Awards, Daily }

    /// <summary>
    /// The title-screen settings row: music, sound effects, haptics and the shell skin. The
    /// same switches live in the MENU (PauseMenuView); these are the quick ones on the title.
    /// Only live on Title, so they never swallow a gameplay tap.
    /// </summary>
    public sealed class TitleToggleView : MonoBehaviour
    {
        [SerializeField] TMP_Text musicLabel;
        [SerializeField] TMP_Text soundLabel;
        [SerializeField] TMP_Text hapticsLabel;
        [SerializeField] TMP_Text skinLabel;
        [Tooltip("1.2.0: Google Play Games leaderboard and achievements; hidden when Play Games is not set up.")]
        [SerializeField] TMP_Text onlineLabel;
        [SerializeField] TMP_Text awardsLabel;
        [SerializeField] SpriteRenderer[] onlineCards = new SpriteRenderer[0];
        [Tooltip("1.2.0: the daily challenge; a tick once today's scored attempt is in.")]
        [SerializeField] TMP_Text dailyLabel;

        bool _dailyDone;
        int _dailyScore;
        string _skinHint;

        /// <summary>The game loop tells the card whether today's challenge is done, and its score.</summary>
        public void SetDaily(bool done, int score)
        {
            _dailyDone = done;
            _dailyScore = score;
            Render();
        }

        /// <summary>What the next locked skin asks for ("ASH 250 A"), or null when all are earned.</summary>
        public void SetSkinHint(string hint)
        {
            _skinHint = hint;
            Render();
        }
        [SerializeField] Vector2 hitSize = new(2.4f, 0.9f);
        [SerializeField] Color onColor = new(1f, 0.788f, 0.4f);
        [SerializeField] Color offColor = new(0.227f, 0.173f, 0.094f);

        SettingsService _settings;
        ProfileService _profile;

        bool _online;

        public void Initialise(SettingsService settings, ProfileService profile, bool online = false)
        {
            _settings = settings;
            _profile = profile;
            _online = online;

            // 1.2.0: long words (ORZECH, RANKING, a skin hint) shrink to fit the note instead of being clipped.
            foreach (TMP_Text label in new[] { musicLabel, soundLabel, hapticsLabel, skinLabel, onlineLabel, awardsLabel, dailyLabel })
            {
                if (label == null || label.enableAutoSizing)
                    continue;

                label.fontSizeMax = label.fontSize;
                label.fontSizeMin = label.fontSize * 0.5f;
                label.enableAutoSizing = true;
                // dark ink vanishes on the wood, so the text box is the paper's width, not the 2.4 slot
                label.rectTransform.sizeDelta = new Vector2(1.35f, label.rectTransform.sizeDelta.y);
            }
            foreach (TMP_Text label in new[] { onlineLabel, awardsLabel })
            {
                if (label != null)
                    label.gameObject.SetActive(online);
            }
            foreach (SpriteRenderer card in onlineCards)
            {
                if (card != null)
                    card.gameObject.SetActive(online);
            }
            Render();
        }

        /// <summary>Screen style: ink on the painted cards.</summary>
        public void SetColors(Color on, Color off)
        {
            onColor = on;
            offColor = off;
            Render();
        }

        /// <summary>
        /// Anything but None means the tap was taken, so the caller does not also start a round.
        /// `world` is the tap already mapped into the LCD scene (LcdPointer).
        /// </summary>
        public TitleTap TryHandleTap(Vector3 world)
        {
            if (_settings == null || !gameObject.activeInHierarchy)
                return TitleTap.None;

            if (Hits(dailyLabel, world))
                return TitleTap.Daily;
            if (_online && Hits(onlineLabel, world))
                return TitleTap.Online;
            if (_online && Hits(awardsLabel, world))
                return TitleTap.Awards;

            if (Hits(musicLabel, world))
                _settings.ToggleMusic();
            else if (Hits(soundLabel, world))
                _settings.ToggleSfx();
            else if (Hits(hapticsLabel, world))
                _settings.ToggleHaptics();
            else if (Hits(skinLabel, world) && _profile != null)
                _profile.SelectNextSkin();
            else
                return TitleTap.None;

            Render();
            return TitleTap.Toggle;
        }

        public void Render()
        {
            if (_settings == null)
                return;

            SetToggle(musicLabel, "♪", _settings.MusicEnabled);
            SetToggle(soundLabel, "SFX", _settings.SfxEnabled);
            SetToggle(hapticsLabel, "~", _settings.HapticsEnabled);

            if (skinLabel != null && _profile != null)
            {
                SkinCatalog.TryGet(_profile.SelectedSkin, out Skin skin);
                skinLabel.text = Loc.Skin(SkinCatalog.IndexOf(_profile.SelectedSkin), skin.Name)
                    + (string.IsNullOrEmpty(_skinHint) ? "" : $"\n<size=42%>{_skinHint}</size>");
                skinLabel.color = onColor;
            }

            if (dailyLabel != null)
            {
                dailyLabel.text = Loc.T(Txt.DailyCard) + (_dailyDone ? $"\n<size=55%>{_dailyScore:000}</size>" : "");
                dailyLabel.color = onColor;
            }

            if (onlineLabel != null)
            {
                onlineLabel.text = Loc.T(Txt.RankingCard);
                onlineLabel.color = onColor;
            }

            if (awardsLabel != null)
            {
                awardsLabel.text = Loc.T(Txt.AwardsCard);
                awardsLabel.color = onColor;
            }
        }

        void SetToggle(TMP_Text label, string glyph, bool on)
        {
            if (label == null)
                return;

            label.text = $"{glyph} {Loc.OnOff(on)}";
            label.color = on ? onColor : offColor;
        }

        bool Hits(TMP_Text label, Vector3 worldPoint) => LcdHit.Hits(label, worldPoint, hitSize);

        /// <summary>
        /// hitSize is authored in the labels' local (LCD) units, the same units the scene
        /// generator lays them out in; the box follows the labels' scale so it can never be
        /// wider than their spacing (which once routed taps to the wrong toggle).
        /// </summary>
        public static Bounds HitBounds(Vector3 centre, Vector3 lossyScale, Vector2 hitSize) =>
            LcdHit.Bounds(centre, lossyScale, hitSize);
    }

    /// <summary>Hit boxes around world-space labels in the LCD scene, shared by the title toggles and the menu.</summary>
    public static class LcdHit
    {
        public static bool Hits(TMP_Text label, Vector3 worldPoint, Vector2 hitSize)
        {
            if (label == null || !label.gameObject.activeInHierarchy)
                return false;

            Vector3 centre = label.transform.position;
            Bounds bounds = Bounds(centre, label.transform.lossyScale, hitSize);
            return bounds.Contains(new Vector3(worldPoint.x, worldPoint.y, centre.z));
        }

        public static Bounds Bounds(Vector3 centre, Vector3 lossyScale, Vector2 hitSize) =>
            new(centre, new Vector3(
                hitSize.x * Mathf.Abs(lossyScale.x),
                hitSize.y * Mathf.Abs(lossyScale.y),
                10f));
    }
}
