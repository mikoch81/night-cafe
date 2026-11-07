namespace NightCafe.Services
{
    /// <summary>Play Games achievements (1.2.0). The exact Play Console names are in docs/store/PLAY_GAMES_SETUP.md.</summary>
    public enum Award
    {
        FirstShift,
        Score100,
        Score250,
        Score500,
        Score1000,
        Streak25,
        Streak50,
        TerribleTen,
        Level5,
        ModeB50,
        PaprikaShooed,
        Count
    }

    public static class Awards
    {
        /// <summary>Steps of the one incremental achievement: shoo Paprika off the stool this many times.</summary>
        public const int PaprikaSteps = 10;

        /// <summary>
        /// Which achievement a resource constant from the Play Console XML is ("achievement_score_100"
        /// for an achievement named "Score 100" in the default language, English; Polish names also match). The console turns letters like ą into "_", so both
        /// spellings are accepted. False for anything else.
        /// </summary>
        public static bool TryMatch(string resourceName, out Award award)
        {
            award = Award.Count;
            const string prefix = "achievement_";
            if (resourceName == null || !resourceName.StartsWith(prefix))
                return false;

            switch (resourceName.Substring(prefix.Length))
            {
                case "first_shift":
                case "pierwsza_zmiana": award = Award.FirstShift; break;
                case "score_100":
                case "wynik_100": award = Award.Score100; break;
                case "score_250":
                case "wynik_250": award = Award.Score250; break;
                case "score_500":
                case "wynik_500": award = Award.Score500; break;
                case "score_1000":
                case "wynik_1000": award = Award.Score1000; break;
                case "streak_25":
                case "seria_25": award = Award.Streak25; break;
                case "streak_50":
                case "seria_50": award = Award.Streak50; break;
                case "terrible_ten":
                case "straszna_dziesiatka":
                case "straszna_dziesi_tka": award = Award.TerribleTen; break;
                case "level_5":
                case "poziom_5": award = Award.Level5; break;
                case "orders_50":
                case "zamowienia_50":
                case "zam_wienia_50": award = Award.ModeB50; break;
                case "paprika_tamer":
                case "pogromca_papriki": award = Award.PaprikaShooed; break;
                default: return false;
            }

            return true;
        }

        /// <summary>The score achievements a total reaches (any mode; the Mode B one only in B).</summary>
        public static void ForScore(int total, bool modeB, System.Collections.Generic.List<Award> into)
        {
            into.Clear();
            if (total >= 100) into.Add(Award.Score100);
            if (total >= 250) into.Add(Award.Score250);
            if (total >= 500) into.Add(Award.Score500);
            if (total >= 1000) into.Add(Award.Score1000);
            if (modeB && total >= 50) into.Add(Award.ModeB50);
        }
    }
}
