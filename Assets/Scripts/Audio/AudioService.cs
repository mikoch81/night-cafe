using NightCafe.Config;
using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Audio
{
    /// <summary>
    /// Plays the synthesised SFX and the lo-fi bed (GDD 5.3).
    /// Unity exposes no scripting API for creating an AudioMixer, so levels are plain
    /// AudioSource volumes; the music offset is applied as a gain multiplier.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] AudioConfig config;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioSource musicSource;

        SettingsService _settings;

        /// <summary>Silences effects only; the lo-fi bed keeps playing under the attract demo.</summary>
        public bool SfxMuted { get; set; }

        public void Initialise(SettingsService settings)
        {
            _settings = settings;
            musicSource.clip = config.lofiLoop;
            musicSource.loop = true;
            ApplySettings();
        }

        public void ApplySettings()
        {
            bool musicOn = _settings?.MusicEnabled ?? true;
            bool sfxOn = _settings?.SfxEnabled ?? true;

            sfxSource.volume = sfxOn ? config.sfxVolume : 0f;
            musicSource.volume = musicOn ? AudioLevels.LinearGain(config.musicOffsetDb) : 0f;

            if (musicOn && !musicSource.isPlaying && musicSource.clip != null)
                musicSource.Play();
            else if (!musicOn && musicSource.isPlaying)
                musicSource.Pause();
        }

        public void Play(GameSfx sfx)
        {
            if (SfxMuted || (_settings != null && !_settings.SfxEnabled))
                return;

            AudioClip clip = config.Clip(sfx);
            if (clip != null)
                sfxSource.PlayOneShot(clip, config.sfxVolume);
        }

        public void StartMusic()
        {
            if (musicSource.clip == null || (_settings != null && !_settings.MusicEnabled))
                return;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }
    }
}
