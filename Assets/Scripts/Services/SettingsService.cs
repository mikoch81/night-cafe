using System;
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
    /// Player toggles for music, sound effects, haptics (GDD 4, 5.3) and segment ghosts (GDD 5.2).
    /// Everything defaults to on except the ghosts, which are an opt-in LCD affectation.
    /// </summary>
    public sealed class SettingsService
    {
        const string MusicKey = "nightcafe.music";
        const string SfxKey = "nightcafe.sfx";
        const string HapticsKey = "nightcafe.haptics";
        const string GhostsKey = "nightcafe.ghosts";
        const string RetroScreenKey = "nightcafe.retro";

        readonly ISettingsStore _store;

        public SettingsService(ISettingsStore store)
        {
            _store = store;
            MusicEnabled = _store.Load(MusicKey, true);
            SfxEnabled = _store.Load(SfxKey, true);
            HapticsEnabled = _store.Load(HapticsKey, true);
            GhostsEnabled = _store.Load(GhostsKey, false);
            RetroScreen = _store.Load(RetroScreenKey, false);
        }

        public bool MusicEnabled { get; private set; }
        public bool SfxEnabled { get; private set; }
        public bool HapticsEnabled { get; private set; }
        public bool GhostsEnabled { get; private set; }

        /// <summary>GDD 5.2a: the segmented Neo-LCD look instead of the painted default.</summary>
        public bool RetroScreen { get; private set; }

        public event Action Changed;

        public void ToggleMusic() => Set(MusicKey, MusicEnabled = !MusicEnabled);

        public void ToggleSfx() => Set(SfxKey, SfxEnabled = !SfxEnabled);

        public void ToggleHaptics() => Set(HapticsKey, HapticsEnabled = !HapticsEnabled);

        public void ToggleGhosts() => Set(GhostsKey, GhostsEnabled = !GhostsEnabled);

        public void ToggleRetroScreen() => Set(RetroScreenKey, RetroScreen = !RetroScreen);

        void Set(string key, bool value)
        {
            _store.Save(key, value);
            Changed?.Invoke();
        }
    }
}
