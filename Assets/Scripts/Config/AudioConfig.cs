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
        LeverClick = 6,
        RushBell = 7,
        LadderCreak = 8,
        LadderBreak = 9,
        LadderKnock = 10,
        CatHiss = 11,
        LevelUp = 12,
        MachineFrenzy = 13
    }

    /// <summary>
    /// Clips and levels (GDD 5.3). The scene's base config holds the levels and haptics; the
    /// screen style's set holds the clips, cut by tools/prep_audio.py from generated recordings
    /// (art/audio/LICENSE.md).
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

        [Header("Café events (1.1.0)")]
        public AudioClip rushBell;
        public AudioClip ladderCreak;
        public AudioClip ladderBreak;
        public AudioClip ladderKnock;
        public AudioClip catHiss;
        public AudioClip levelUp;
        [Tooltip("An espresso machine going haywire: the terrible ten seconds (level 5+).")]
        public AudioClip machineFrenzy;
        [Tooltip("Rush-hour walla: short takes scattered at random while it lasts.")]
        public AudioClip[] rushCrowd = new AudioClip[0];
        [Tooltip("Level of the walla takes relative to the SFX, in decibels.")]
        public float rushCrowdOffsetDb = -8f;
        [Tooltip("The room tone comes up by this much during a rush hour, in decibels.")]
        public float rushAmbienceBoostDb = 6f;

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
            GameSfx.RushBell => rushBell,
            GameSfx.LadderCreak => ladderCreak,
            GameSfx.LadderBreak => ladderBreak,
            GameSfx.LadderKnock => ladderKnock,
            GameSfx.CatHiss => catHiss,
            GameSfx.LevelUp => levelUp,
            GameSfx.MachineFrenzy => machineFrenzy,
            _ => null
        };
    }
}
