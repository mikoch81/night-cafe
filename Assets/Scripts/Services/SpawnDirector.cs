using System.Collections.Generic;
using NightCafe.Config;
using NightCafe.Core;

namespace NightCafe.Services
{
    /// <summary>
    /// What the spawner needs to know about cups currently in play.
    /// </summary>
    public interface ILaneOccupancy
    {
        int TotalActiveCups { get; }

        int CupsOnLane(int laneIndex);

        /// <summary>
        /// Step index (0 = K1) of the most recently spawned cup on the lane,
        /// or int.MaxValue when the lane is empty. Cups never overtake each other,
        /// so the newest cup is always the one closest to K1.
        /// </summary>
        int NewestCupStep(int laneIndex);
    }

    /// <summary>
    /// Pure spawn decisions per GDD 2.3: interval rolling and lane eligibility.
    /// </summary>
    public sealed class SpawnDirector
    {
        readonly SpawnSettings _settings;
        readonly IRandom _rng;
        readonly List<int> _eligible = new(LanePositionExtensions.Count);

        public SpawnDirector(in SpawnSettings settings, IRandom rng)
        {
            _settings = settings;
            _rng = rng;
        }

        public float FirstSpawnDelay => _settings.FirstSpawnDelay;

        /// <summary>Seconds until the next spawn: stepTime x R, R weighted over {2, 3, 4}.</summary>
        public float RollNextInterval(float stepTime)
        {
            int multiplier = _rng.WeightedPick(_settings.IntervalMultipliers, _settings.IntervalWeights);
            return stepTime * multiplier;
        }

        /// <summary>
        /// Picks a lane that can accept a new cup, honouring the per-lane cup cap,
        /// the minimum step gap, and the on-screen limit for the current tempo level.
        /// Returns false when nothing may spawn right now; the caller retries next frame
        /// so a blocked spawn happens as soon as a slot frees up.
        /// </summary>
        public bool TryPickLane(ILaneOccupancy occupancy, int tempoLevel, out int laneIndex)
        {
            laneIndex = -1;

            if (occupancy.TotalActiveCups >= _settings.ScreenLimitFor(tempoLevel))
                return false;

            _eligible.Clear();
            for (int lane = 0; lane < LanePositionExtensions.Count; lane++)
            {
                int cups = occupancy.CupsOnLane(lane);
                if (cups >= _settings.MaxCupsPerLane)
                    continue;

                if (cups > 0 && occupancy.NewestCupStep(lane) < _settings.MinStepGap)
                    continue;

                _eligible.Add(lane);
            }

            if (_eligible.Count == 0)
                return false;

            laneIndex = _rng.UniformPick(_eligible);
            return true;
        }
    }
}
