using System.Collections.Generic;
using System.Text;
using NightCafe.Core;
using NightCafe.Services;
using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>What a tap on the menu card asks for; the game loop carries it out.</summary>
    public enum MenuAction
    {
        None,
        Resume,
        ToggleMusic,
        ToggleSound,
        FlipMode,
        ShowScores,
        Back,
        EndShift,
        Quit,
        ToggleLanguage
    }

    /// <summary>
    /// The MENU card (review 2026-09-26): a paper card over the dimmed screen, opened by the
    /// MENU pill on the body or the Android back button, in every state. Rows: resume, music,
    /// sound, mode A/B, the local top 10, end shift and quit. A second page lists the scores.
    /// The view only draws and hit-tests; GameLoopController decides what each row does.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text header;
        [SerializeField] TMP_Text resumeRow;
        [SerializeField] TMP_Text musicRow;
        [SerializeField] TMP_Text soundRow;
        [SerializeField] TMP_Text modeRow;
        [SerializeField] TMP_Text scoresRow;
        [SerializeField] TMP_Text endShiftRow;
        [SerializeField] TMP_Text quitRow;
        [SerializeField] TMP_Text languageRow;
        [SerializeField] GameObject mainPage;
        [SerializeField] GameObject scoresPage;
        [SerializeField] TMP_Text scoresText;
        [SerializeField] TMP_Text backRow;
        [Tooltip("Tap box around a row, in LCD units.")]
        [SerializeField] Vector2 rowHitSize = new(5.6f, 0.8f);
        [SerializeField] Color ink = new(0.23f, 0.16f, 0.11f);
        [SerializeField] Color fadedInk = new(0.63f, 0.55f, 0.45f);

        bool _inRound;

        public bool IsOpen => root != null && root.activeSelf;

        public bool ShowingScores => IsOpen && scoresPage != null && scoresPage.activeSelf;

        void Awake()
        {
            if (root != null)
                root.SetActive(false);
        }

        /// <summary>Screen style: the ink of the paper props.</summary>
        public void SetColors(Color on, Color off)
        {
            ink = on;
            fadedInk = off;
        }

        /// <param name="inRound">A live shift is paused under the card: RESUME and END SHIFT apply.</param>
        public void Open(bool inRound, SettingsService settings, GameMode mode)
        {
            _inRound = inRound;
            if (root != null)
                root.SetActive(true);
            ShowMain();
            Render(settings, mode);
        }

        public void Close()
        {
            if (root != null)
                root.SetActive(false);
        }

        public void ShowMain()
        {
            SetActive(mainPage, true);
            SetActive(scoresPage, false);
        }

        public void Render(SettingsService settings, GameMode mode)
        {
            Set(header, Loc.T(_inRound ? Txt.Paused : Txt.Menu), ink);
            Set(resumeRow, Loc.T(_inRound ? Txt.Resume : Txt.Close), ink);
            Set(musicRow, $"{Loc.T(Txt.Music)}  {Loc.OnOff(settings.MusicEnabled)}", settings.MusicEnabled ? ink : fadedInk);
            Set(soundRow, $"{Loc.T(Txt.Sound)}  {Loc.OnOff(settings.SfxEnabled)}", settings.SfxEnabled ? ink : fadedInk);
            Set(modeRow, $"{Loc.T(Txt.Mode)}  {mode.Letter()}", ink);
            Set(scoresRow, Loc.T(Txt.TopTen), ink);
            Set(languageRow, Loc.T(Txt.LanguageRow), ink);
            Set(endShiftRow, Loc.T(Txt.EndShift), _inRound ? ink : fadedInk);
            Set(quitRow, Loc.T(Txt.QuitGame), ink);
        }

        /// <summary>The local list for one mode, best first, with a BACK row under it.</summary>
        public void ShowScores(GameMode mode, IReadOnlyList<ScoreEntry> scores)
        {
            SetActive(mainPage, false);
            SetActive(scoresPage, true);
            Set(header, $"{Loc.T(Txt.TopTen)} · {mode.Letter()}", ink);
            Set(backRow, Loc.T(Txt.Back), ink);
            if (scoresText != null)
            {
                scoresText.text = FormatScores(scores);
                scoresText.color = ink;
            }
        }

        public static string FormatScores(IReadOnlyList<ScoreEntry> scores)
        {
            if (scores == null || scores.Count == 0)
                return Loc.T(Txt.NoShiftsYet);

            var text = new StringBuilder();
            for (int i = 0; i < scores.Count && i < ProfileService.TopScoreCount; i++)
            {
                if (i > 0)
                    text.Append('\n');
                string date = string.IsNullOrEmpty(scores[i].date) ? "" : "   " + scores[i].date;
                text.Append($"{i + 1,2}.  {scores[i].score:000}{date}");
            }

            return text.ToString();
        }

        /// <summary>Which row a tap in the LCD scene landed on; taps elsewhere on the card do nothing.</summary>
        public MenuAction Hit(Vector3 lcdPoint)
        {
            if (!IsOpen)
                return MenuAction.None;

            if (ShowingScores)
                return LcdHit.Hits(backRow, lcdPoint, rowHitSize) ? MenuAction.Back : MenuAction.None;

            if (LcdHit.Hits(resumeRow, lcdPoint, rowHitSize)) return MenuAction.Resume;
            if (LcdHit.Hits(musicRow, lcdPoint, rowHitSize)) return MenuAction.ToggleMusic;
            if (LcdHit.Hits(soundRow, lcdPoint, rowHitSize)) return MenuAction.ToggleSound;
            if (LcdHit.Hits(modeRow, lcdPoint, rowHitSize)) return MenuAction.FlipMode;
            if (LcdHit.Hits(scoresRow, lcdPoint, rowHitSize)) return MenuAction.ShowScores;
            if (LcdHit.Hits(languageRow, lcdPoint, rowHitSize)) return MenuAction.ToggleLanguage;
            if (_inRound && LcdHit.Hits(endShiftRow, lcdPoint, rowHitSize)) return MenuAction.EndShift;
            if (LcdHit.Hits(quitRow, lcdPoint, rowHitSize)) return MenuAction.Quit;
            return MenuAction.None;
        }

        static void Set(TMP_Text text, string value, Color colour)
        {
            if (text == null)
                return;

            text.text = value;
            text.color = colour;
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }
    }
}
