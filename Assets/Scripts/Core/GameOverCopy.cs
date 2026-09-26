namespace NightCafe.Core
{
    /// <summary>
    /// The words on the end-of-shift card, as four separate lines so the view can set each in
    /// its own type: a heading, the big counter, the record line and the footer with the
    /// restart prompt. Pure, so the copy is testable without a scene.
    /// </summary>
    public readonly struct GameOverCopy
    {
        public readonly string Header;
        public readonly string Score;
        public readonly string Record;
        public readonly string Footer;

        /// <summary>True when the record line celebrates a new best (drawn in the accent ink).</summary>
        public readonly bool NewRecord;

        GameOverCopy(string header, string score, string record, string footer, bool newRecord)
        {
            Header = header;
            Score = score;
            Record = record;
            Footer = footer;
            NewRecord = newRecord;
        }

        /// <param name="rank">Place on the local top-10 list, 0 when the shift did not make it.</param>
        public static GameOverCopy Build(int displayScore, int bestDisplay, bool newRecord, string unlockedSkin, int rank = 0)
        {
            string record = newRecord ? Loc.T(Txt.NewBest)
                : rank > 0 ? Loc.F(Txt.OnList, rank, bestDisplay)
                : Loc.F(Txt.BestScore, bestDisplay);
            string restart = Loc.T(Txt.TapToRestart);
            string footer = string.IsNullOrEmpty(unlockedSkin)
                ? restart
                : Loc.F(Txt.SkinUnlocked, unlockedSkin.ToUpperInvariant()) + "\n" + restart;
            return new GameOverCopy(Loc.T(Txt.EndOfShift), displayScore.ToString("000"), record, footer, newRecord);
        }
    }
}
