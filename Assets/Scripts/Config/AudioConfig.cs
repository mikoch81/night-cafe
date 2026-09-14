using UnityEngine;

namespace NightCafe.Config
{
    public enum GameSfx
    {
        Catch = 0,
        ComboBonus = 1,
        Miss = 2,
        CatMeow = 3,
        GameOver = 4,
        BrewAlarm = 5,
        LeverClick = 6
    }

    /// <summary>
    /// Clips and levels (GDD 5.3). One per screen style: the RETRO set is synthesised by
    /// tools/gen_audio.py, the ART set is cut by tools/prep_audio.py from generated recordings
    /// (art/audio/LICENSE.md). Haptics are read from the scene's base config only.
    /// </summary>
    [CreateAssetMenu(menuName = "NightCafe/Audio Config", fileName = "AudioConfig")]
    public sealed class AudioConfig : ScriptableObject
    {
        [Header("SFX")]
        public AudioClip catchBlip;
        public AudioClip comboArpeggio;
        public AudioClip missClink;
        public AudioClip catMeow;
        public AudioClip gameOver;
        public AudioClip brewAlarm;
        public AudioClip leverClick;

        [Header("Music")]
        public AudioClip lofiLoop;

        [Tooltip("Music level relative to the SFX, in decibels (GDD 5.3 asks for -18).")]
        public float musicOffsetDb = -18f;

        [Tooltip("Room tone under the music (rain on the window, the café); follows the music toggle. Empty = none.")]
        public AudioClip ambience;

        [Tooltip("Ambience level relative to the SFX, in decibels.")]
        public float ambienceOffsetDb = -23f;

        [Range(0f, 1f)] public float sfxVolume = 1f;

        [Tooltip("Chance the cat meows when it crosses (GDD 5.3: rarely, 30% of transitions).")]
        [Range(0f, 1f)] public float meowChance = 0.30f;

        [Header("Haptics, milliseconds (GDD 4)")]
        public int catchHapticMs = 15;
        public int missHapticMs = 60;
        public int gameOverHapticMs = 80;
        public int gameOverHapticPulses = 3;
        public int gameOverHapticGapMs = 60;

        public AudioClip Clip(GameSfx sfx) => sfx switch
        {
            GameSfx.Catch => catchBlip,
            GameSfx.ComboBonus => comboArpeggio,
            GameSfx.Miss => missClink,
            GameSfx.CatMeow => catMeow,
            GameSfx.GameOver => gameOver,
            GameSfx.BrewAlarm => brewAlarm,
            GameSfx.LeverClick => leverClick,
            _ => null
        };
    }
}
