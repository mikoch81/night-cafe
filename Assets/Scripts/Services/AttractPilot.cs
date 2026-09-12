using System.Collections.Generic;
using NightCafe.Core;

namespace NightCafe.Services
{
    /// <summary>What the pilot can see of one cup in flight.</summary>
    public readonly struct PilotCup
    {
        public readonly int Id;
        public readonly int Lane;
        public readonly int Step;
        public readonly bool Wanted;

        public PilotCup(int id, int lane, int step, bool wanted)
        {
            Id = id;
            Lane = lane;
            Step = step;
            Wanted = wanted;
        }
    }

    /// <summary>
    /// The attract-mode player (GDD 6): stands under the wanted cup closest to K5, steps aside
    /// when an unwanted colour is about to land on its tray, reacts with a human-ish delay, and
    /// deliberately fumbles a few cups so the demo ends on its own instead of running forever.
    /// Pure, so the fumble rate and reaction time are testable.
    /// </summary>
    public sealed class AttractPilot
    {
        readonly IRandom _rng;
        readonly float _reactionSeconds;
        readonly float _fumbleChance;
        readonly int _catchStep;
        readonly HashSet<int> _seen = new();
        readonly HashSet<int> _fumbled = new();
        readonly List<int> _gone = new();
        int _target = -1;
        float _reaction;

        /// <param name="catchStep">Step index of K5; a cup one step before it lands next.</param>
        public AttractPilot(IRandom rng, float reactionSeconds, float fumbleChance, int catchStep)
        {
            _rng = rng;
            _reactionSeconds = reactionSeconds;
            _fumbleChance = fumbleChance;
            _catchStep = catchStep;
        }

        /// <summary>Lane to move to this frame, or -1 to stay put.</summary>
        public int Decide(IReadOnlyList<PilotCup> cups, int currentLane, float deltaSeconds)
        {
            Forget(cups);

            int wantedLane = -1;
            int wantedStep = -1;
            bool threatened = false;

            for (int i = 0; i < cups.Count; i++)
            {
                PilotCup cup = cups[i];
                if (_seen.Add(cup.Id) && _rng.NextFloat() < _fumbleChance)
                    _fumbled.Add(cup.Id);

                if (!cup.Wanted)
                {
                    threatened |= cup.Lane == currentLane && cup.Step >= _catchStep - 1;
                    continue;
                }

                if (_fumbled.Contains(cup.Id) || cup.Step <= wantedStep)
                    continue;

                wantedStep = cup.Step;
                wantedLane = cup.Lane;
            }

            int goal = wantedLane >= 0 ? wantedLane : threatened ? SafeLane(cups, currentLane) : -1;

            if (goal < 0 || goal == currentLane)
            {
                _target = -1;
                return -1;
            }

            if (goal != _target)
            {
                _target = goal;
                _reaction = 0f;
            }

            _reaction += deltaSeconds;
            if (_reaction < _reactionSeconds)
                return -1;

            _target = -1;
            return goal;
        }

        public void Reset()
        {
            _seen.Clear();
            _fumbled.Clear();
            _target = -1;
            _reaction = 0f;
        }

        /// <summary>Nearest lane with no unwanted cup about to land; the current lane if there is none.</summary>
        int SafeLane(IReadOnlyList<PilotCup> cups, int currentLane)
        {
            for (int lane = 0; lane < LanePositionExtensions.Count; lane++)
            {
                if (lane == currentLane)
                    continue;

                bool safe = true;
                for (int i = 0; i < cups.Count && safe; i++)
                    safe = cups[i].Wanted || cups[i].Lane != lane || cups[i].Step < _catchStep - 1;

                if (safe)
                    return lane;
            }

            return currentLane;
        }

        /// <summary>Drops bookkeeping for cups that have landed, so the sets stay bounded.</summary>
        void Forget(IReadOnlyList<PilotCup> cups)
        {
            _gone.Clear();
            foreach (int id in _seen)
            {
                bool present = false;
                for (int i = 0; i < cups.Count && !present; i++)
                    present = cups[i].Id == id;

                if (!present)
                    _gone.Add(id);
            }

            for (int i = 0; i < _gone.Count; i++)
            {
                _seen.Remove(_gone[i]);
                _fumbled.Remove(_gone[i]);
            }
        }
    }
}
