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
        void SignInSilently();

        /// <summary>From the menu: asks the player to sign in; `done` gets whether it worked.</summary>
        void SignIn(Action<bool> done);

        /// <summary>At the end of a shift; dropped silently when not signed in or offline.</summary>
        void Submit(GameMode mode, int score);

        /// <summary>Google's own leaderboard screen for the mode, over the game.</summary>
        void ShowBoard(GameMode mode);
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
        public void SignInSilently() { }
        public void SignIn(Action<bool> done) => done?.Invoke(false);
        public void Submit(GameMode mode, int score) { }
        public void ShowBoard(GameMode mode) { }
    }
}
