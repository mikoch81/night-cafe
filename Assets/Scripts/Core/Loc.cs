using System;
using UnityEngine;

namespace NightCafe.Core
{
    public enum Language
    {
        English,
        Polish
    }

    /// <summary>Every line the game shows, by meaning. Formats take their numbers in {0}, {1}.</summary>
    public enum Txt
    {
        TitleTapToStart,
        DemoTapToStart,
        EndOfShift,
        TapToRestart,
        NewBest,
        /// <summary>The record on the receipt: {0} = the score.</summary>
        BestScore,
        /// <summary>{0} = place on the list, {1} = the record.</summary>
        OnList,
        /// <summary>{0} = the skin's name.</summary>
        SkinUnlocked,
        /// <summary>The HUD's record, in the lettering font - no diacritics there. {0} = the score.</summary>
        HudBest,
        /// <summary>The HUD's level, in the lettering font - no diacritics there. {0} = the level.</summary>
        HudLevel,
        Paused,
        Menu,
        Resume,
        Close,
        Music,
        Sound,
        On,
        Off,
        Mode,
        TopTen,
        OnlineTopTen,
        EndShift,
        QuitGame,
        NoShiftsYet,
        Back,
        LanguageRow,
        /// <summary>{0} = the level.</summary>
        LevelBanner,
        RushHour,
        QuietSpell,
        FullSpeed,
        TerribleTen,
        Brew,
        Count
    }

    /// <summary>
    /// The English and Polish copy (1.1.0, review 2026-09-26). Pure lookup; the language comes
    /// from the settings (first run: the phone's language). Lines that land in the chalk
    /// lettering font (Cabin Sketch: the score board's BEST and LV) keep to ASCII - it has no
    /// Polish diacritics; everything else is set in Patrick Hand, which has them all.
    /// </summary>
    public static class Loc
    {
        static readonly string[] English = new string[(int)Txt.Count];
        static readonly string[] Polish = new string[(int)Txt.Count];

        static readonly string[] SkinsEn = { "WALNUT", "ASH", "ONYX", "NEON" };
        static readonly string[] SkinsPl = { "ORZECH", "JESION", "ONYKS", "NEON" };
        static readonly string[] OrdersEn = { "ESPRESSO", "CARAMEL", "LATTE", "DECAF" };
        static readonly string[] OrdersPl = { "ESPRESSO", "KARMEL", "LATTE", "BEZ KOFEINY" };

        static Loc()
        {
            Add(Txt.TitleTapToStart, "NIGHT CAFÉ\nTAP TO START", "NIGHT CAFÉ\nDOTKNIJ, BY ZACZĄĆ");
            Add(Txt.DemoTapToStart, "DEMO - TAP TO START", "DEMO - DOTKNIJ, BY ZACZĄĆ");
            Add(Txt.EndOfShift, "END OF SHIFT", "KONIEC ZMIANY");
            Add(Txt.TapToRestart, "TAP TO RESTART", "DOTKNIJ: NOWA ZMIANA");
            Add(Txt.NewBest, "NEW BEST!", "NOWY REKORD!");
            Add(Txt.BestScore, "BEST {0:000}", "REKORD {0:000}");
            Add(Txt.OnList, "#{0} ON LIST · BEST {1:000}", "#{0} NA LIŚCIE · REKORD {1:000}");
            Add(Txt.SkinUnlocked, "{0} UNLOCKED", "{0} ODBLOKOWANY");
            Add(Txt.HudBest, "BEST {0:000}", "REKORD {0:000}");
            Add(Txt.HudLevel, "LV {0}", "POZ {0}");
            Add(Txt.Paused, "PAUSED", "PAUZA");
            Add(Txt.Menu, "MENU", "MENU");
            Add(Txt.Resume, "RESUME", "WZNÓW");
            Add(Txt.Close, "CLOSE", "ZAMKNIJ");
            Add(Txt.Music, "MUSIC", "MUZYKA");
            Add(Txt.Sound, "SOUND", "DŹWIĘKI");
            Add(Txt.On, "ON", "WŁ.");
            Add(Txt.Off, "OFF", "WYŁ.");
            Add(Txt.Mode, "MODE", "TRYB");
            Add(Txt.TopTen, "TOP 10", "TOP 10");
            Add(Txt.OnlineTopTen, "ONLINE TOP 10", "RANKING ONLINE");
            Add(Txt.EndShift, "END SHIFT", "ZAKOŃCZ ZMIANĘ");
            Add(Txt.QuitGame, "QUIT GAME", "WYJDŹ Z GRY");
            Add(Txt.NoShiftsYet, "NO SHIFTS YET", "JESZCZE ŻADNEJ ZMIANY");
            Add(Txt.Back, "BACK", "WRÓĆ");
            Add(Txt.LanguageRow, "LANGUAGE  EN", "JĘZYK  PL");
            Add(Txt.LevelBanner, "LEVEL {0}", "POZIOM {0}");
            Add(Txt.RushHour, "RUSH HOUR", "GODZINA SZCZYTU");
            Add(Txt.QuietSpell, "QUIET SPELL", "CHWILA SPOKOJU");
            Add(Txt.FullSpeed, "FULL SPEED", "PEŁNA PARA");
            Add(Txt.TerribleTen, "TERRIBLE TEN", "STRASZNA DZIESIĄTKA");
            Add(Txt.Brew, "BREW", "PARZENIE");
        }

        static void Add(Txt key, string english, string polish)
        {
            English[(int)key] = english;
            Polish[(int)key] = polish;
        }

        public static Language Current { get; private set; } = Language.English;

        /// <summary>Raised after the language changes, so views re-render their copy.</summary>
        public static event Action Changed;

        public static void Set(Language language)
        {
            if (language == Current)
                return;

            Current = language;
            Changed?.Invoke();
        }

        /// <summary>The first-run default: Polish on a Polish phone, English everywhere else.</summary>
        public static Language FromSystem(SystemLanguage system) =>
            system == SystemLanguage.Polish ? Language.Polish : Language.English;

        public static string T(Txt key) => (Current == Language.Polish ? Polish : English)[(int)key];

        public static string F(Txt key, params object[] args) => string.Format(T(key), args);

        /// <summary>A setting's state word, ON / WŁ.</summary>
        public static string OnOff(bool on) => T(on ? Txt.On : Txt.Off);

        /// <summary>A shell skin's name by its catalogue index (walnut, ash, onyx, neon).</summary>
        public static string Skin(int index, string fallback) => Pick(SkinsEn, SkinsPl, index, fallback);

        /// <summary>A Mode B order's name by colour index (espresso, caramel, latte, decaf).</summary>
        public static string Order(int index, string fallback) => Pick(OrdersEn, OrdersPl, index, fallback);

        static string Pick(string[] en, string[] pl, int index, string fallback)
        {
            string[] table = Current == Language.Polish ? pl : en;
            return index >= 0 && index < table.Length ? table[index] : fallback;
        }

        /// <summary>Every key has a line in both languages (a test checks this).</summary>
        public static bool Complete(out Txt missing)
        {
            for (int i = 0; i < (int)Txt.Count; i++)
            {
                if (string.IsNullOrEmpty(English[i]) || string.IsNullOrEmpty(Polish[i]))
                {
                    missing = (Txt)i;
                    return false;
                }
            }

            missing = Txt.Count;
            return true;
        }
    }
}
