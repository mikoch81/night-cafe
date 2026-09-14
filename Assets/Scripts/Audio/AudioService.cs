using NightCafe.Config;
using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Audio
{
    /// <summary>
    /// Plays the SFX, the lo-fi bed and the room tone (GDD 5.3). The clips come from the
    /// current sound set - each screen style brings its own, the chiptune for RETRO and the
    /// recordings for ART - while levels and haptics stay with the scene's base config.
    /// Unity exposes no scripting API for creating an AudioMixer, so levels are plain
    /// AudioSource volumes; the offsets are applied as gain multipliers.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] AudioConfig config;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource ambienceSource;

        SettingsService _settings;
        AudioConfig _set;

        AudioConfig Set => _set != null ? _set : config;

        /// <summary>Silences effects only; the lo-fi bed keeps playing under the attract demo.</summary>
        public bool SfxMuted { get; set; }

        public void Initialise(SettingsService settings)
        {
            _settings = settings;
            LoadBeds();
            ApplySettings();
        }

        /// <summary>
        /// Swaps the clips for a screen style's set (null = the base config). A running bed is
        /// restarted on the new clip; the room tone appears or vanishes with it.
        /// </summary>
        public void SetSoundSet(AudioConfig set)
        {
            if (set == _set)
                return;

            bool wasPlaying = musicSource.isPlaying;
            _set = set;
            LoadBeds();
            ApplySettings();
            if (wasPlaying)
                StartMusic();
        }

        void LoadBeds()
        {
            AudioConfig set = Set;
            if (musicSource.clip != set.lofiLoop)
            {
                musicSource.Stop();
                musicSource.clip = set.lofiLoop;
            }
            musicSource.loop = true;

            if (ambienceSource == null)
                return;
            if (ambienceSource.clip != set.ambience)
            {
                ambienceSource.Stop();
                ambienceSource.clip = set.ambience;
            }
            ambienceSource.loop = true;
        }

        public void ApplySettings()
        {
            bool musicOn = _settings?.MusicEnabled ?? true;
            bool sfxOn = _settings?.SfxEnabled ?? true;

            sfxSource.volume = sfxOn ? config.sfxVolume : 0f;
            musicSource.volume = musicOn ? AudioLevels.LinearGain(config.musicOffsetDb) : 0f;
            if (ambienceSource != null)
                ambienceSource.volume = musicOn ? AudioLevels.LinearGain(config.ambienceOffsetDb) : 0f;

            if (musicOn)
                StartMusic();
            else
            {
                if (musicSource.isPlaying) musicSource.Pause();
                if (ambienceSource != null && ambienceSource.isPlaying) ambienceSource.Pause();
            }
        }

        public void Play(GameSfx sfx)
        {
            if (SfxMuted || (_settings != null && !_settings.SfxEnabled))
                return;

            AudioClip clip = Set.Clip(sfx);
            if (clip != null)
                sfxSource.PlayOneShot(clip, config.sfxVolume);
        }

        /// <summary>The bed and the room tone together; either is skipped when its set has none.</summary>
        public void StartMusic()
        {
            if (_settings != null && !_settings.MusicEnabled)
                return;

            if (musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
            if (ambienceSource != null && ambienceSource.clip != null && !ambienceSource.isPlaying)
                ambienceSource.Play();
        }
    }
}
