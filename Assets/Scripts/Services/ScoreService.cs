using System;
using NightCafe.Config;

namespace NightCafe.Services
{
    /// <summary>
    /// Score, combo, and the score-driven events: cat mercy (GDD 2.5), breather (2.6), rollover (2.7).
    /// Thresholds are detected by comparing the score before and after an award, never by equality,
    /// so a +5 combo bonus that jumps over a threshold (199 -> 205) still triggers it exactly once.
    /// </summary>
    public sealed class ScoreService
    {
        readonly ScoreSettings _settings;
        int _total;
        int _combo;

        public ScoreService(in ScoreSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Uncapped running score; keeps counting past a rollover.</summary>
        public int TotalScore => _total;

        /// <summary>What the three-digit counter shows (GDD 2.7).</summary>
        public int DisplayScore => _total % _settings.RolloverModulo;

        public int Combo => _combo;

        public event Action<int> ScoreChanged;
        public event Action ComboBonusAwarded;
        public event Action MercyTriggered;
        public event Action BreatherTriggered;
        public event Action RolloverOccurred;

        public void RegisterCatch()
        {
            int before = _total;

            _total += _settings.PointsPerCatch;
            _combo++;

            bool bonus = _combo % _settings.ComboBonusEvery == 0;
            if (bonus)
                _total += _settings.ComboBonusPoints;

            ScoreChanged?.Invoke(DisplayScore);

            if (bonus)
                ComboBonusAwarded?.Invoke();

            RaiseThresholdEvents(before, _total);
        }

        public void RegisterMiss()
        {
            _combo = 0;
        }

        public void Reset()
        {
            _total = 0;
            _combo = 0;
            ScoreChanged?.Invoke(DisplayScore);
        }

        void RaiseThresholdEvents(int before, int after)
        {
            int period = _settings.RolloverModulo;

            // Mercy thresholds re-arm on every counter cycle: 200, 500, 1200, 1500, ...
            foreach (int threshold in _settings.MercyThresholds)
            {
                for (int i = 0; i < Crossings(before, after, threshold, period); i++)
                    MercyTriggered?.Invoke();
            }

            for (int i = 0; i < Crossings(before, after, _settings.BreatherEvery, _settings.BreatherEvery); i++)
                BreatherTriggered?.Invoke();

            for (int i = 0; i < Crossings(before, after, period, period); i++)
                RolloverOccurred?.Invoke();
        }

        /// <summary>
        /// How many values of the form (offset + k * period) lie in the interval (before, after].
        /// </summary>
        static int Crossings(int before, int after, int offset, int period)
        {
            if (period <= 0)
                return 0;

            return FloorDiv(after - offset, period) - FloorDiv(before - offset, period);
        }

        static int FloorDiv(int a, int b)
        {
            int quotient = a / b;
            if (a % b != 0 && (a < 0) != (b < 0))
                quotient--;

            return quotient;
        }
    }
}
