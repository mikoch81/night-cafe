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
