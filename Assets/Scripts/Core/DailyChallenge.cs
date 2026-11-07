using System;
using System.Globalization;

namespace NightCafe.Core
{
    /// <summary>
    /// 1.2.0 daily challenge: one shift a day with the same randomness for every player, keyed by
    /// the UTC date so the whole world shares a day (and the Play Games "today" board).
    /// </summary>
    public static class DailyChallenge
    {
        public static string Today => Key(DateTime.UtcNow);

        public static string Key(DateTime utc) => utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>yyyyMMdd as a number: 2026-11-07 -> 20261107.</summary>
        public static int Seed(string key) =>
            int.Parse(key.Replace("-", ""), NumberStyles.None, CultureInfo.InvariantCulture);
    }
}
