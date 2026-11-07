using System;
using System.Text;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
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

        public void SignInSilently(Action<bool> done = null) =>
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                _signedIn = status == SignInStatus.Success;
                done?.Invoke(_signedIn);
            });

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

        const string SaveName = "profile";
        bool _cloudRead;
        bool _saving;
        string _pendingSave;

        public void LoadProfile(Action<string> done)
        {
            if (!_signedIn)
            {
                done?.Invoke(null);
                return;
            }

            OpenSave(meta =>
            {
                if (meta == null)
                {
                    done?.Invoke(null);
                    return;
                }

                PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(meta, (status, bytes) =>
                {
                    _cloudRead = status == SavedGameRequestStatus.Success;
                    bool any = _cloudRead && bytes != null && bytes.Length > 0;
                    done?.Invoke(any ? Encoding.UTF8.GetString(bytes) : null);
                });
            });
        }

        /// <summary>Saves come in bursts (a score and a skin in one frame): one write at a time, the newest wins.</summary>
        public void SaveProfile(string json)
        {
            if (!_signedIn || !_cloudRead)
                return;

            _pendingSave = json;
            if (!_saving)
                CommitPending();
        }

        void CommitPending()
        {
            byte[] data = Encoding.UTF8.GetBytes(_pendingSave);
            _pendingSave = null;
            _saving = true;

            OpenSave(meta =>
            {
                if (meta == null)
                {
                    _saving = false;
                    return;
                }

                SavedGameMetadataUpdate update = new SavedGameMetadataUpdate.Builder()
                    .WithUpdatedDescription("Night Café").Build();
                PlayGamesPlatform.Instance.SavedGame.CommitUpdate(meta, update, data, (status, _) =>
                {
                    _saving = false;
                    if (status != SavedGameRequestStatus.Success)
                        Debug.LogWarning($"[NightCafe] Play Games did not keep the profile: {status}.");
                    if (_pendingSave != null)
                        CommitPending();
                });
            });
        }

        /// <summary>Conflicts (two phones offline) take the newer copy; the older one's device merges itself back in at its next launch.</summary>
        static void OpenSave(Action<ISavedGameMetadata> then) =>
            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(SaveName,
                DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseMostRecentlySaved, (status, meta) =>
                {
                    if (status != SavedGameRequestStatus.Success)
                        Debug.LogWarning($"[NightCafe] Play Games saved game not opened: {status}.");
                    then(status == SavedGameRequestStatus.Success ? meta : null);
                });

        static string BoardOf(GameMode mode) =>
            mode == GameMode.B ? PlayGamesIds.LeaderboardModeB : PlayGamesIds.LeaderboardModeA;
    }
}
