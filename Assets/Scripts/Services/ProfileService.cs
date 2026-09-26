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

        /// <summary>
        /// Writes beside the file and swaps it in, so a process kill mid-write (Android does
        /// that to backgrounded apps) leaves the previous profile intact instead of a truncated one.
        /// </summary>
        public void Save(string json)
        {
            string temp = _path + ".tmp";
            try
            {
                File.WriteAllText(temp, json);

                if (File.Exists(_path))
                    File.Replace(temp, _path, null);
                else
                    File.Move(temp, _path);
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

    /// <summary>One finished shift on the local score list.</summary>
    [Serializable]
    public struct ScoreEntry
    {
        public int mode;
        public int score;
        /// <summary>Local date, yyyy-MM-dd.</summary>
        public string date;

        public ScoreEntry(GameMode mode, int score, DateTime when)
        {
            this.mode = (int)mode;
            this.score = score;
            date = when.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Serialised shape; public fields because JsonUtility ignores properties.</summary>
    [Serializable]
    public sealed class ProfileData
    {
        /// <summary>2: the local top-10 list per mode (1.1.0). A schema-1 file simply has no list yet.</summary>
        public const int CurrentSchema = 2;

        /// <summary>Bumped when the shape changes, so a later build can migrate instead of guessing.</summary>
        public int schemaVersion = CurrentSchema;

        public int[] bestScores = new int[GameModeExtensions.Count];
        public List<ScoreEntry> topScores = new();
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

        /// <summary>Length of the local score list, per mode.</summary>
        public const int TopScoreCount = 10;

        /// <summary>Records a finished round; true when it set a new record.</summary>
        public bool SubmitScore(GameMode mode, int totalScore) => SubmitScore(mode, totalScore, DateTime.Now, out _);

        /// <summary>
        /// Records a finished round on the record and the local top list. `rank` is its place
        /// on the list (1 = best), or 0 when it did not make the list. Returns true on a new record.
        /// A zero-point shift is not worth a line.
        /// </summary>
        public bool SubmitScore(GameMode mode, int totalScore, DateTime when, out int rank)
        {
            rank = 0;
            bool record = totalScore > Best(mode);
            if (record)
                _data.bestScores[(int)mode] = totalScore;

            if (totalScore > 0)
                rank = InsertTopScore(new ScoreEntry(mode, totalScore, when));

            if (record || rank > 0)
                Persist();
            return record;
        }

        /// <summary>The mode's list, best first.</summary>
        public IReadOnlyList<ScoreEntry> TopScores(GameMode mode)
        {
            var list = new List<ScoreEntry>(TopScoreCount);
            foreach (ScoreEntry entry in _data.topScores)
            {
                if (entry.mode == (int)mode)
                    list.Add(entry);
            }

            return list;
        }

        /// <summary>Keeps the list sorted (ties: the older shift stays ahead) and trimmed per mode.</summary>
        int InsertTopScore(ScoreEntry entry)
        {
            int rank = 1;
            int at = _data.topScores.Count;
            for (int i = 0; i < _data.topScores.Count; i++)
            {
                ScoreEntry other = _data.topScores[i];
                if (other.mode != entry.mode)
                    continue;

                if (other.score >= entry.score)
                {
                    rank++;
                    continue;
                }

                at = i;
                break;
            }

            if (rank > TopScoreCount)
                return 0;

            _data.topScores.Insert(at, entry);
            TrimTopScores(entry.mode);
            return rank;
        }

        void TrimTopScores(int mode)
        {
            int kept = 0;
            for (int i = 0; i < _data.topScores.Count; i++)
            {
                if (_data.topScores[i].mode != mode)
                    continue;

                if (++kept > TopScoreCount)
                    _data.topScores.RemoveAt(i--);
            }
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
            data.topScores ??= new List<ScoreEntry>();
            if (data.schemaVersion < ProfileData.CurrentSchema)
            {
                // A 1.0.0 profile only knew the records: they open the list, dated unknown.
                for (int mode = 0; mode < data.bestScores.Length; mode++)
                {
                    if (data.bestScores[mode] > 0 && !data.topScores.Exists(e => e.mode == mode))
                        data.topScores.Add(new ScoreEntry { mode = mode, score = data.bestScores[mode], date = "" });
                }

                data.schemaVersion = ProfileData.CurrentSchema;
            }
            data.selectedSkin ??= "";
            return data;
        }
    }
}
