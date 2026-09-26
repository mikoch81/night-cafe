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
            string record = newRecord ? "NEW BEST!"
                : rank > 0 ? $"#{rank} ON LIST · BEST {bestDisplay:000}"
                : $"BEST {bestDisplay:000}";
            string footer = string.IsNullOrEmpty(unlockedSkin)
                ? "TAP TO RESTART"
                : $"{unlockedSkin.ToUpperInvariant()} UNLOCKED\nTAP TO RESTART";
            return new GameOverCopy("END OF SHIFT", displayScore.ToString("000"), record, footer, newRecord);
        }
    }
}
