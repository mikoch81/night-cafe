namespace NightCafe.PlayGames
{
    /// <summary>
    /// The leaderboard ids from Play Console (Play Games Services → Leaderboards → Get resources,
    /// docs/store/PLAY_GAMES_SETUP.md, step 6). Not secrets. Empty until the console is set up:
    /// the online board then simply does not exist and the game stays offline.
    /// The app id goes through the plugin's own setup (GameInfo.cs and its manifest library).
    /// </summary>
    public static class PlayGamesIds
    {
        public const string LeaderboardModeA = "";
        public const string LeaderboardModeB = "";
    }
}
