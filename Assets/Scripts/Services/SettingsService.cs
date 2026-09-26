using System;
using NightCafe.Core;
using System.Collections.Generic;
using UnityEngine;

namespace NightCafe.Services
{
    public interface ISettingsStore
    {
        bool Load(string key, bool fallback);
        void Save(string key, bool value);
    }

    /// <summary>Runtime store. M3's SaveService can supply a JSON-backed one without touching this class.</summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public bool Load(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;

        public void Save(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public sealed class InMemorySettingsStore : ISettingsStore
    {
        readonly Dictionary<string, bool> _values = new();

        public bool Load(string key, bool fallback) => _values.TryGetValue(key, out bool value) ? value : fallback;

        public void Save(string key, bool value) => _values[key] = value;
    }

    /// <summary>
    /// Player toggles for music, sound effects and haptics (GDD 4, 5.3). Everything defaults to on.
    /// </summary>
    public sealed class SettingsService
    {
        const string MusicKey = "nightcafe.music";
        const string SfxKey = "nightcafe.sfx";
        const string HapticsKey = "nightcafe.haptics";
        const string PolishKey = "nightcafe.polish";

        readonly ISettingsStore _store;

        /// <param name="systemLanguage">The language on a first run (the phone's); a saved choice wins.</param>
        public SettingsService(ISettingsStore store, Language systemLanguage = Language.English)
        {
            _store = store;
            MusicEnabled = _store.Load(MusicKey, true);
            SfxEnabled = _store.Load(SfxKey, true);
            HapticsEnabled = _store.Load(HapticsKey, true);
            Language = _store.Load(PolishKey, systemLanguage == Language.Polish) ? Language.Polish : Language.English;
        }

        public bool MusicEnabled { get; private set; }
        public bool SfxEnabled { get; private set; }
        public bool HapticsEnabled { get; private set; }
        public Language Language { get; private set; }

        public event Action Changed;

        public void ToggleMusic() => Set(MusicKey, MusicEnabled = !MusicEnabled);

        public void ToggleSfx() => Set(SfxKey, SfxEnabled = !SfxEnabled);

        public void ToggleHaptics() => Set(HapticsKey, HapticsEnabled = !HapticsEnabled);

        /// <summary>English and Polish, one tap each way (review 2026-09-26).</summary>
        public void ToggleLanguage()
        {
            Language = Language == Language.Polish ? Language.English : Language.Polish;
            Set(PolishKey, Language == Language.Polish);
        }

        void Set(string key, bool value)
        {
            _store.Save(key, value);
            Changed?.Invoke();
        }
    }
}
