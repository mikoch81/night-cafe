using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using NightCafe.Core;
using NightCafe.Services;
using UnityEngine;

namespace NightCafe.PlayGames
{
    /// <summary>
    /// <see cref="IOnlineScores"/> on Google Play Games Services v2 (plugin 2.2.1). Installs
    /// itself before the first scene, on Android devices only and only once the ids are set -
    /// otherwise the offline backend stays and nothing touches the network.
    /// </summary>
    public sealed class PlayGamesScores : IOnlineScores
    {
        bool _signedIn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!GameInfo.ApplicationIdInitialized() || string.IsNullOrEmpty(PlayGamesIds.LeaderboardModeA))
                return;

            PlayGamesPlatform.Activate();
            OnlineScores.Backend = new PlayGamesScores();
#endif
        }

        public bool Available => true;

        public bool SignedIn => _signedIn;

        public void SignInSilently() =>
            PlayGamesPlatform.Instance.Authenticate(status => _signedIn = status == SignInStatus.Success);

        public void SignIn(Action<bool> done) =>
            PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
            {
                _signedIn = status == SignInStatus.Success;
                done?.Invoke(_signedIn);
            });

        public void Submit(GameMode mode, int score)
        {
            if (!_signedIn || score <= 0)
                return;

            PlayGamesPlatform.Instance.ReportScore(score, BoardOf(mode), ok =>
            {
                if (!ok)
                    Debug.LogWarning($"[NightCafe] Play Games did not take the {mode} score {score}.");
            });
        }

        public void ShowBoard(GameMode mode) => PlayGamesPlatform.Instance.ShowLeaderboardUI(BoardOf(mode));

        static string BoardOf(GameMode mode) =>
            mode == GameMode.B ? PlayGamesIds.LeaderboardModeB : PlayGamesIds.LeaderboardModeA;
    }
}
