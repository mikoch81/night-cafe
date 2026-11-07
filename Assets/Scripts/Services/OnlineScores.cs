using System;
using NightCafe.Core;

namespace NightCafe.Services
{
    /// <summary>
    /// The online top 10 (stage 3 of 1.1.0: Google Play Games leaderboards, one per mode). The
    /// game only talks to this interface; the Play Games implementation lives in its own
    /// assembly (NightCafe.PlayGames, Android + Editor) and installs itself before the first
    /// scene. Without it - or before the Play Console ids are in - everything here is a quiet
    /// no-op and the game stays fully offline, the local top 10 as before.
    /// </summary>
    public interface IOnlineScores
    {
        /// <summary>The Play Console ids are configured: the online board exists at all.</summary>
        bool Available { get; }

        bool SignedIn { get; }

        /// <summary>At launch: signs in without asking if the player already uses Play Games.</summary>
        void SignInSilently(Action<bool> done = null);

        /// <summary>From the menu: asks the player to sign in; `done` gets whether it worked.</summary>
        void SignIn(Action<bool> done);

        /// <summary>At the end of a shift; dropped silently when not signed in or offline.</summary>
        void Submit(GameMode mode, int score);

        /// <summary>Google's own leaderboard screen for the mode, over the game.</summary>
        void ShowBoard(GameMode mode);

        /// <summary>
        /// After sign-in: the profile kept in Play Games saved games (1.1.1), so records and skins
        /// survive a reinstall or a new phone. `done` gets null when there is none or it failed.
        /// </summary>
        void LoadProfile(Action<string> done);

        /// <summary>
        /// Keeps the profile in the cloud. Ignored until LoadProfile has read the cloud copy this
        /// session, so a fresh install cannot overwrite the saved history with an empty profile.
        /// </summary>
        void SaveProfile(string json);

        /// <summary>1.2.0: unlocks an achievement; dropped when signed out or not set up in Play Console.</summary>
        void Unlock(Award award);

        /// <summary>1.2.0: a step towards an incremental achievement (Paprika shooed).</summary>
        void Increment(Award award, int steps);

        /// <summary>Google's achievements screen over the game.</summary>
        void ShowAwards();
    }

    public static class OnlineScores
    {
        /// <summary>Replaced by NightCafe.PlayGames on Android; offline elsewhere (and in tests).</summary>
        public static IOnlineScores Backend { get; set; } = new OfflineScores();
    }

    public sealed class OfflineScores : IOnlineScores
    {
        public bool Available => false;
        public bool SignedIn => false;
        public void SignInSilently(Action<bool> done = null) => done?.Invoke(false);
        public void SignIn(Action<bool> done) => done?.Invoke(false);
        public void Submit(GameMode mode, int score) { }
        public void ShowBoard(GameMode mode) { }
        public void LoadProfile(Action<string> done) => done?.Invoke(null);
        public void SaveProfile(string json) { }
        public void Unlock(Award award) { }
        public void Increment(Award award, int steps) { }
        public void ShowAwards() { }
    }
}
