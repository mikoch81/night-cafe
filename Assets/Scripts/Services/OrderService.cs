using System;
using NightCafe.Config;
using NightCafe.Core;

namespace NightCafe.Services
{
    /// <summary>
    /// Mode B orders (GDD 3): which colour the bar wants right now, which colour a new cup
    /// gets, and when the order rotates - after N correct catches or T seconds, whichever
    /// comes first. Disabled orders mean Mode A: every cup is colour 0 and always wanted.
    /// </summary>
    public sealed class OrderService
    {
        readonly OrderSettings _settings;
        readonly IRandom _rng;
        readonly float[] _weights;
        readonly int[] _colours;
        int _correctCatches;
        float _elapsed;

        public OrderService(in OrderSettings settings, IRandom rng)
        {
            _settings = settings;
            _rng = rng;

            _colours = new int[settings.ColourCount];
            _weights = new float[settings.ColourCount];
            for (int i = 0; i < _colours.Length; i++)
                _colours[i] = i;
        }

        public bool Enabled => _settings.Enabled;

        /// <summary>Colour index the bar currently wants; always 0 when orders are disabled.</summary>
        public int CurrentOrder { get; private set; }

        public int CorrectCatches => _correctCatches;

        public float SecondsSinceChange => _elapsed;

        public event Action<int> OrderChanged;

        public bool IsWanted(int colour) => !Enabled || colour == CurrentOrder;

        /// <summary>Colour for a cup about to spawn: the order at OrderedWeight, the rest split evenly.</summary>
        public int RollCupColour()
        {
            if (!Enabled || _colours.Length == 1)
                return 0;

            float other = (1f - _settings.OrderedWeight) / (_colours.Length - 1);
            for (int i = 0; i < _weights.Length; i++)
                _weights[i] = i == CurrentOrder ? _settings.OrderedWeight : other;

            return _rng.WeightedPick(_colours, _weights);
        }

        public void RegisterCorrectCatch()
        {
            if (!Enabled)
                return;

            _correctCatches++;
            if (_correctCatches >= _settings.ChangeAfterCatches)
                Rotate();
        }

        public void Tick(float deltaSeconds)
        {
            if (!Enabled)
                return;

            _elapsed += deltaSeconds;
            if (_elapsed >= _settings.ChangeAfterSeconds)
                Rotate();
        }

        /// <summary>Starts a round: fresh counters and a freshly rolled order.</summary>
        public void Reset()
        {
            _correctCatches = 0;
            _elapsed = 0f;

            int previous = CurrentOrder;
            CurrentOrder = Enabled ? _rng.NextInt(0, _colours.Length) : 0;

            if (CurrentOrder != previous)
                OrderChanged?.Invoke(CurrentOrder);
        }

        /// <summary>A new order is always a different colour, so the panel visibly changes (GDD 3).</summary>
        void Rotate()
        {
            _correctCatches = 0;
            _elapsed = 0f;

            if (_colours.Length > 1)
            {
                int offset = _rng.NextInt(1, _colours.Length);
                CurrentOrder = (CurrentOrder + offset) % _colours.Length;
            }

            OrderChanged?.Invoke(CurrentOrder);
        }
    }

    public enum CatchOutcome
    {
        /// <summary>Wanted cup landed on the tray.</summary>
        Caught,

        /// <summary>Wanted cup hit the floor.</summary>
        Missed,

        /// <summary>Unwanted colour landed on the tray: penalised like a miss (GDD 3).</summary>
        WrongCatch,

        /// <summary>Unwanted colour passed by: the correct thing to do, nothing happens.</summary>
        Ignored
    }

    public static class CatchRules
    {
        public static CatchOutcome Resolve(bool baristaPresent, bool cupWanted)
        {
            if (cupWanted)
                return baristaPresent ? CatchOutcome.Caught : CatchOutcome.Missed;

            return baristaPresent ? CatchOutcome.WrongCatch : CatchOutcome.Ignored;
        }

        /// <summary>Both a miss and a wrong catch cost a stain and break the combo.</summary>
        public static bool IsPenalised(CatchOutcome outcome) =>
            outcome is CatchOutcome.Missed or CatchOutcome.WrongCatch;
    }
}
