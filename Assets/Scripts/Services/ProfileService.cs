using System;
using System.Collections.Generic;
using System.IO;
using NightCafe.Core;
using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>Where the profile JSON lives; null means nothing saved yet.</summary>
    public interface IProfileStore
    {
        string Load();
        void Save(string json);
    }

    /// <summary>GDD 6: highscores as JSON in Application.persistentDataPath.</summary>
    public sealed class FileProfileStore : IProfileStore
    {
        readonly string _path;

        public FileProfileStore(string path)
        {
            _path = path;
        }

        public static FileProfileStore Default() =>
            new(Path.Combine(Application.persistentDataPath, "profile.json"));

        public string Load()
        {
            try
            {
                return File.Exists(_path) ? File.ReadAllText(_path) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NightCafe] Could not read profile: {e.Message}");
                return null;
            }
        }

        public void Save(string json)
        {
            try
            {
                File.WriteAllText(_path, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NightCafe] Could not save profile: {e.Message}");
            }
        }
    }

    public sealed class InMemoryProfileStore : IProfileStore
    {
        public string Json { get; private set; }

        public InMemoryProfileStore(string json = null)
        {
            Json = json;
        }

        public string Load() => Json;

        public void Save(string json) => Json = json;
    }

    /// <summary>Serialised shape; public fields because JsonUtility ignores properties.</summary>
    [Serializable]
    public sealed class ProfileData
    {
        public int[] bestScores = new int[GameModeExtensions.Count];
        public List<string> unlockedSkins = new();
        public string selectedSkin = "";
        public int selectedMode;
    }

    /// <summary>
    /// Persistent per-player state: highscore per mode and the shell skins (GDD 6).
    /// Scores are compared on the uncapped total, so a 1 050 run beats a 999 one
    /// even though the counter showed 050 at the end.
    /// </summary>
    public sealed class ProfileService
    {
        readonly IProfileStore _store;
        readonly ProfileData _data;

        public ProfileService(IProfileStore store)
        {
            _store = store;
            _data = Parse(store.Load());
        }

        public event Action Changed;

        public int Best(GameMode mode) => _data.bestScores[(int)mode];

        public GameMode SelectedMode =>
            _data.selectedMode >= 0 && _data.selectedMode < GameModeExtensions.Count
                ? (GameMode)_data.selectedMode
                : GameMode.A;

        public void SelectMode(GameMode mode)
        {
            if (mode == SelectedMode)
                return;

            _data.selectedMode = (int)mode;
            Persist();
        }

        /// <summary>Records a finished round; true when it set a new record.</summary>
        public bool SubmitScore(GameMode mode, int totalScore)
        {
            if (totalScore <= Best(mode))
                return false;

            _data.bestScores[(int)mode] = totalScore;
            Persist();
            return true;
        }

        public bool IsUnlocked(string skinId) =>
            SkinCatalog.IsDefault(skinId) || _data.unlockedSkins.Contains(skinId);

        /// <summary>True the first time a skin is unlocked, so the caller can celebrate once.</summary>
        public bool Unlock(string skinId)
        {
            if (IsUnlocked(skinId))
                return false;

            _data.unlockedSkins.Add(skinId);
            Persist();
            return true;
        }

        public string SelectedSkin =>
            IsUnlocked(_data.selectedSkin) && SkinCatalog.TryGet(_data.selectedSkin, out _)
                ? _data.selectedSkin
                : SkinCatalog.DefaultId;

        public void SelectSkin(string skinId)
        {
            if (!IsUnlocked(skinId) || skinId == _data.selectedSkin)
                return;

            _data.selectedSkin = skinId;
            Persist();
        }

        /// <summary>Cycles to the next unlocked skin in catalogue order.</summary>
        public string SelectNextSkin()
        {
            IReadOnlyList<Skin> all = SkinCatalog.All;
            int start = SkinCatalog.IndexOf(SelectedSkin);

            for (int step = 1; step <= all.Count; step++)
            {
                string candidate = all[(start + step) % all.Count].Id;
                if (IsUnlocked(candidate))
                {
                    SelectSkin(candidate);
                    break;
                }
            }

            return SelectedSkin;
        }

        void Persist()
        {
            _store.Save(JsonUtility.ToJson(_data));
            Changed?.Invoke();
        }

        static ProfileData Parse(string json)
        {
            var data = new ProfileData();
            if (string.IsNullOrEmpty(json))
                return data;

            try
            {
                JsonUtility.FromJsonOverwrite(json, data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NightCafe] Profile JSON unreadable, starting fresh: {e.Message}");
                data = new ProfileData();
            }

            // A file written by a build with fewer modes must not throw on the new ones.
            if (data.bestScores == null || data.bestScores.Length != GameModeExtensions.Count)
                Array.Resize(ref data.bestScores, GameModeExtensions.Count);

            data.unlockedSkins ??= new List<string>();
            data.selectedSkin ??= "";
            return data;
        }
    }
}
