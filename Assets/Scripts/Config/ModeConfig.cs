using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Config
{
    /// <summary>
    /// Every balance number for a game mode. Values default to Mode A per GDD; nothing is hardcoded in logic.
    /// </summary>
    [CreateAssetMenu(menuName = "NightCafe/Mode Config", fileName = "ModeConfig")]
    public sealed class ModeConfig : ScriptableObject
    {
        [Header("Tempo (GDD 2.2)")]
        public float baseStepTime = 0.90f;
        public float stepTimeDecrement = 0.055f;
        public float minStepTime = 0.40f;
        public int catchesPerTempoLevel = 12;
        public int maxTempoLevel = 9;
        public int startTempoLevel;

        [Header("Spawner (GDD 2.3)")]
        public float firstSpawnDelay = 1.2f;
        public int[] intervalMultipliers = { 2, 3, 4 };
        public float[] intervalWeights = { 0.20f, 0.50f, 0.30f };
        public int maxCupsPerLane = 2;
        public int minStepGap = 2;

        [Tooltip("Max cups on screen, indexed by tempo level T. GDD: T0-T2 = 2, T3-T5 = 3, T6+ = 4.")]
        public int[] screenLimitByTempo = { 2, 2, 2, 3, 3, 3, 4, 4, 4, 4 };

        [Header("Score (GDD 2.4, 2.6, 2.7)")]
        public int pointsPerCatch = 1;
        public int comboBonusEvery = 25;
        public int comboBonusPoints = 5;
        public int rolloverModulo = 1000;

        [Tooltip("Displayed-score thresholds where the cat wipes one stain. Re-arm after every rollover.")]
        public int[] mercyThresholds = { 200, 500 };

        public int breatherEvery = 100;
        public float breatherDuration = 2.5f;

        [Header("Penalty (GDD 2.5)")]
        public int maxStains = 3;

        [Header("Presentation timings (GDD 2.4, 2.5)")]
        public float catchPoseDuration = 0.12f;
        public float missPoseDuration = 0.30f;
        public float brokenCupDuration = 0.40f;
        public float catCrossingDuration = 1.6f;

        [Header("Breather (GDD 2.6) and rollover (GDD 2.7) presentation")]
        [Tooltip("The barista alternates wipe/down poses at this period while spawns pause")]
        public float breatherWipePeriod = 0.4f;
        [Tooltip("How many times the bar neon blinks during the breather")]
        public int breatherNeonBlinks = 3;
        [Tooltip("Seconds for the city neons to arrange themselves into the cat after 999")]
        public float rolloverAnimationSeconds = 2f;

        [Header("Skin unlock (GDD 6) - Mode A: Ash at 250, Mode B: Onyx at 500; Neon is the rollover")]
        public string unlockSkinId = "ash";
        public int unlockSkinScore = 250;

        [Header("Orders (GDD 3, Mode B only)")]
        [Tooltip("Off for Mode A: every cup is wanted and cups keep the default tint.")]
        public bool ordersEnabled;

        [Tooltip("Cup tints in order: espresso, caramel, latte, decaf (GDD 3).")]
        public Color[] orderColors =
        {
            new(1f, 0.788f, 0.4f),     // #ffc966 espresso
            new(1f, 0.616f, 0.431f),   // #ff9d6e caramel
            new(1f, 0.914f, 0.659f),   // #ffe9a8 latte
            new(0.851f, 0.549f, 1f)    // #d98cff decaf
        };

        public string[] orderNames = { "ESPRESSO", "CARAMEL", "LATTE", "DECAF" };

        [Tooltip("Spawn weight of the ordered colour; every other colour shares the rest equally.")]
        [Range(0f, 1f)] public float orderedColorWeight = 0.55f;

        [Tooltip("The order rotates after this many correct catches ...")]
        public int orderChangeCatches = 10;

        [Tooltip("... or after this many seconds, whichever comes first.")]
        public float orderChangeSeconds = 20f;

        [Header("Levels and café events (1.1.0, review 2026-09-26)")]
        [Tooltip("Total score that opens levels 2, 3, 4, 5. Mode B scores 2 a cup, so it doubles these.")]
        public int[] levelThresholds = { 25, 50, 100, 140 };
        [Tooltip("Points per level after the last threshold (the numbers stop growing there: level 5 is flat out).")]
        public int levelEvery = 50;
        public int catOnLadderFromLevel = 2;
        public int laddersFromLevel = 3;
        public int rushHourFromLevel = 4;
        [Tooltip("From this level on the gaps between events shrink, a little each level.")]
        public int eventsSpeedUpFromLevel = 3;
        [Tooltip("Seconds between ladder / cat events, counted after the last one is over.")]
        public float eventGap = 14f;
        public float eventGapShrinkPerLevel = 3f;
        [Tooltip("The floor, reached at level 5: never closer than this.")]
        public float eventGapMin = 8f;
        [Tooltip("Seconds after a level unlocks the cat or the ladders before the first one turns up.")]
        public float eventIntroDelay = 3f;
        [Tooltip("Seconds of the rush level before its first rush hour.")]
        public float firstRushDelay = 10f;
        public float rushHourSeconds = 60f;
        [Tooltip("Seconds from the end of one rush hour to the next.")]
        public float rushHourGap = 90f;
        public float rushHourGapShrinkPerLevel = 15f;
        public float rushHourGapMin = 60f;
        [Tooltip("Cups slide this much faster during a rush hour (step time x factor).")]
        [Range(0.5f, 1f)] public float rushStepFactor = 0.85f;
        [Tooltip("Extra cups allowed on screen during a rush hour.")]
        public int rushExtraCups = 1;
        [Tooltip("Below this level a rush hour holds the ladder and cat events back; from it on they overlap.")]
        public int eventsOverlapFromLevel = 5;

        [Tooltip("From this level on, now and then, a quiet spell: slower cups for a few points, then flat out again.")]
        public int quietFromLevel = 5;
        [Tooltip("Points a quiet spell lasts.")]
        public int quietPoints = 20;
        [Tooltip("Points of full speed between quiet spells: a random number in this range.")]
        public int quietGapMinPoints = 40;
        public int quietGapMaxPoints = 80;
        [Tooltip("Step time x factor during a quiet spell (above 1 = slower).")]
        [Range(1f, 2f)] public float quietStepFactor = 1.35f;
        [Tooltip("Cups on screen during a quiet spell, against the tempo's limit (negative = fewer).")]
        public int quietExtraCups = -1;

        [Header("The terrible ten seconds (level 5+, review 2026-09-26)")]
        [Tooltip("From this level on, now and then one espresso machine goes haywire: steam everywhere, cups at full blast.")]
        public int frenzyFromLevel = 5;
        [Tooltip("Seconds of that level before the first one.")]
        public float firstFrenzyDelay = 15f;
        public float frenzySeconds = 10f;
        [Tooltip("Seconds from the end of one to the next, at its first level ...")]
        public float frenzyGap = 75f;
        [Tooltip("... this much shorter every level after ...")]
        public float frenzyGapShrinkPerLevel = 5f;
        [Tooltip("... never shorter than this.")]
        public float frenzyGapMin = 50f;
        [Tooltip("After one, the cat and Noir wait at least this long: a breath.")]
        public float frenzyBreather = 5f;
        [Tooltip("The wild machine's cups: step time x this at its first level (0.65 = 35 % faster) ...")]
        [Range(0.3f, 1f)] public float frenzyStepFactor = 0.65f;
        [Tooltip("... this much faster every level after ...")]
        public float frenzyStepFactorPerLevel = 0.05f;
        [Tooltip("... up to this (0.5 = twice the speed).")]
        [Range(0.3f, 1f)] public float frenzyStepFactorMin = 0.5f;
        [Tooltip("It fires a new cup as soon as the last one is this many steps down the rail.")]
        public int frenzyStepGap = 2;
        [Tooltip("Meanwhile the other machines keep at most this many cups on screen.")]
        public int frenzyOtherCups = 1;
        [Tooltip("A creaking ladder breaks if Miro stands on it this long without stepping off.")]
        public float ladderCreakSeconds = 6f;
        public float ladderBreakAfterSeconds = 2f;
        [Tooltip("A broken ladder is gone this long; its lane sends no cups meanwhile.")]
        public float ladderBrokenSeconds = 10f;
        public float ladderSlowSeconds = 12f;
        [Tooltip("Seconds Miro needs to climb an askew ladder.")]
        public float ladderClimbDelay = 0.45f;
        public int ladderRepairPresses = 2;
        [Tooltip("A knocked-over ladder is set back up by the café after this long even if nobody presses.")]
        public float ladderToppledMaxSeconds = 20f;
        public float catOnLadderSeconds = 12f;

        public EventSettings Events => new(
            levelThresholds, levelEvery, catOnLadderFromLevel, rushHourFromLevel, laddersFromLevel, eventsSpeedUpFromLevel,
            eventGap, eventGapShrinkPerLevel, eventGapMin, firstRushDelay, rushHourSeconds, rushHourGap,
            rushHourGapShrinkPerLevel, rushHourGapMin, eventsOverlapFromLevel, ladderCreakSeconds,
            ladderBreakAfterSeconds, ladderBrokenSeconds, ladderSlowSeconds, ladderClimbDelay,
            ladderRepairPresses, ladderToppledMaxSeconds, catOnLadderSeconds, eventIntroDelay,
            quietFromLevel, quietPoints, quietGapMinPoints, quietGapMaxPoints,
            frenzyFromLevel, firstFrenzyDelay, frenzySeconds, frenzyGap, frenzyGapShrinkPerLevel, frenzyGapMin,
            frenzyBreather);

        /// <summary>How much faster the wild machine fires at `level`: it gets worse with every level.</summary>
        public float FrenzyStepFactorAt(int level) =>
            Mathf.Max(frenzyStepFactorMin, frenzyStepFactor - frenzyStepFactorPerLevel * Mathf.Max(0, level - frenzyFromLevel));

        public TempoSettings Tempo => new(
            baseStepTime, stepTimeDecrement, minStepTime, catchesPerTempoLevel, maxTempoLevel, startTempoLevel);

        public ScoreSettings Score => new(
            pointsPerCatch, comboBonusEvery, comboBonusPoints, rolloverModulo,
            mercyThresholds, breatherEvery);

        public SpawnSettings Spawn => new(
            firstSpawnDelay, intervalMultipliers, intervalWeights,
            maxCupsPerLane, minStepGap, screenLimitByTempo);

        public OrderSettings Orders => new(
            ordersEnabled, orderColors?.Length ?? 0, orderedColorWeight,
            orderChangeCatches, orderChangeSeconds);
    }

    public readonly struct OrderSettings
    {
        public readonly bool Enabled;
        public readonly int ColourCount;
        public readonly float OrderedWeight;
        public readonly int ChangeAfterCatches;
        public readonly float ChangeAfterSeconds;

        public OrderSettings(bool enabled, int colourCount, float orderedWeight,
            int changeAfterCatches, float changeAfterSeconds)
        {
            Enabled = enabled;
            ColourCount = Mathf.Max(1, colourCount);
            OrderedWeight = Mathf.Clamp01(orderedWeight);
            ChangeAfterCatches = Mathf.Max(1, changeAfterCatches);
            ChangeAfterSeconds = Mathf.Max(0.01f, changeAfterSeconds);
        }
    }

    public readonly struct TempoSettings
    {
        public readonly float BaseStepTime;
        public readonly float StepTimeDecrement;
        public readonly float MinStepTime;
        public readonly int CatchesPerLevel;
        public readonly int MaxLevel;
        public readonly int StartLevel;

        public TempoSettings(float baseStepTime, float decrement, float minStepTime,
            int catchesPerLevel, int maxLevel, int startLevel)
        {
            BaseStepTime = baseStepTime;
            StepTimeDecrement = decrement;
            MinStepTime = minStepTime;
            CatchesPerLevel = Mathf.Max(1, catchesPerLevel);
            MaxLevel = maxLevel;
            StartLevel = startLevel;
        }
    }

    public readonly struct ScoreSettings
    {
        public readonly int PointsPerCatch;
        public readonly int ComboBonusEvery;
        public readonly int ComboBonusPoints;
        public readonly int RolloverModulo;
        public readonly int[] MercyThresholds;
        public readonly int BreatherEvery;

        public ScoreSettings(int pointsPerCatch, int comboBonusEvery, int comboBonusPoints,
            int rolloverModulo, int[] mercyThresholds, int breatherEvery)
        {
            PointsPerCatch = pointsPerCatch;
            ComboBonusEvery = Mathf.Max(1, comboBonusEvery);
            ComboBonusPoints = comboBonusPoints;
            RolloverModulo = Mathf.Max(1, rolloverModulo);
            MercyThresholds = mercyThresholds ?? new int[0];
            BreatherEvery = Mathf.Max(1, breatherEvery);
        }
    }

    public readonly struct SpawnSettings
    {
        public readonly float FirstSpawnDelay;
        public readonly int[] IntervalMultipliers;
        public readonly float[] IntervalWeights;
        public readonly int MaxCupsPerLane;
        public readonly int MinStepGap;
        public readonly int[] ScreenLimitByTempo;

        public SpawnSettings(float firstSpawnDelay, int[] multipliers, float[] weights,
            int maxCupsPerLane, int minStepGap, int[] screenLimitByTempo)
        {
            FirstSpawnDelay = firstSpawnDelay;
            IntervalMultipliers = multipliers;
            IntervalWeights = weights;
            MaxCupsPerLane = maxCupsPerLane;
            MinStepGap = minStepGap;
            ScreenLimitByTempo = screenLimitByTempo;
        }

        public int ScreenLimitFor(int tempoLevel)
        {
            if (ScreenLimitByTempo == null || ScreenLimitByTempo.Length == 0)
                return int.MaxValue;

            int index = Mathf.Clamp(tempoLevel, 0, ScreenLimitByTempo.Length - 1);
            return ScreenLimitByTempo[index];
        }
    }
}
