using System;
using NightCafe.Core;
using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>What can happen in the café during a shift (1.1.0, review 2026-09-26).</summary>
    public enum ShiftEvent
    {
        None,
        /// <summary>Paprika, the ginger stray, naps on a stool; the first press on that button shoos him off.</summary>
        CatOnLadder,
        /// <summary>A minute of full house: faster cups, one more on screen.</summary>
        RushHour,
        /// <summary>Noir, the black cat, bumps the stool Miro stands on: it shakes, stay too long and it breaks.</summary>
        LadderCreak,
        /// <summary>Noir bumps a stool askew; climbing it takes a moment.</summary>
        LadderSlowClimb,
        /// <summary>Noir knocks a stool over; a few presses set it back up.</summary>
        LadderRepair,
        /// <summary>The terrible ten seconds: one espresso machine goes haywire and fires cups at full blast.</summary>
        MachineFrenzy
    }

    /// <summary>The level and event numbers of a mode (ModeConfig).</summary>
    public readonly struct EventSettings
    {
        /// <summary>Total scores that open levels 2, 3, ... (ascending).</summary>
        public readonly int[] LevelThresholds;
        /// <summary>Points per level past the last threshold.</summary>
        public readonly int LevelEvery;
        public readonly int CatFromLevel;
        public readonly int RushFromLevel;
        public readonly int LaddersFromLevel;
        public readonly int SpeedUpFromLevel;
        public readonly float EventGap;
        public readonly float EventGapShrink;
        public readonly float EventGapMin;
        public readonly float FirstRushDelay;
        public readonly float RushDuration;
        public readonly float RushGap;
        public readonly float RushGapShrink;
        public readonly float RushGapMin;
        public readonly int OverlapFromLevel;
        public readonly float LadderCreakSeconds;
        public readonly float LadderBreakAfter;
        public readonly float LadderBrokenSeconds;
        public readonly float LadderSlowSeconds;
        public readonly float LadderClimbDelay;
        public readonly int LadderRepairPresses;
        public readonly float LadderToppledMaxSeconds;
        public readonly float CatOnLadderSeconds;
        public readonly float IntroDelay;
        public readonly int QuietFromLevel;
        public readonly int QuietPoints;
        public readonly int QuietGapMin;
        public readonly int QuietGapMax;
        public readonly int FrenzyFromLevel;
        public readonly float FirstFrenzyDelay;
        public readonly float FrenzySeconds;
        public readonly float FrenzyGap;
        public readonly float FrenzyGapShrink;
        public readonly float FrenzyGapMin;
        public readonly float FrenzyBreather;

        public EventSettings(int[] levelThresholds, int levelEvery, int catFromLevel, int rushFromLevel, int laddersFromLevel,
            int speedUpFromLevel, float eventGap, float eventGapShrink, float eventGapMin,
            float firstRushDelay, float rushDuration, float rushGap, float rushGapShrink, float rushGapMin,
            int overlapFromLevel, float ladderCreakSeconds, float ladderBreakAfter, float ladderBrokenSeconds,
            float ladderSlowSeconds, float ladderClimbDelay, int ladderRepairPresses,
            float ladderToppledMaxSeconds, float catOnLadderSeconds, float introDelay,
            int quietFromLevel, int quietPoints, int quietGapMin, int quietGapMax,
            int frenzyFromLevel = int.MaxValue, float firstFrenzyDelay = 15f, float frenzySeconds = 10f, float frenzyGap = 75f,
            float frenzyGapShrink = 0f, float frenzyGapMin = 75f, float frenzyBreather = 5f)
        {
            LevelThresholds = levelThresholds ?? Array.Empty<int>();
            LevelEvery = Mathf.Max(1, levelEvery);
            CatFromLevel = catFromLevel;
            RushFromLevel = rushFromLevel;
            LaddersFromLevel = laddersFromLevel;
            SpeedUpFromLevel = speedUpFromLevel;
            EventGap = Mathf.Max(1f, eventGap);
            EventGapShrink = Mathf.Max(0f, eventGapShrink);
            EventGapMin = Mathf.Max(1f, eventGapMin);
            FirstRushDelay = Mathf.Max(0f, firstRushDelay);
            RushDuration = Mathf.Max(1f, rushDuration);
            RushGap = Mathf.Max(1f, rushGap);
            RushGapShrink = Mathf.Max(0f, rushGapShrink);
            RushGapMin = Mathf.Max(1f, rushGapMin);
            OverlapFromLevel = overlapFromLevel;
            LadderCreakSeconds = Mathf.Max(0.5f, ladderCreakSeconds);
            LadderBreakAfter = Mathf.Max(0.1f, ladderBreakAfter);
            LadderBrokenSeconds = Mathf.Max(0.5f, ladderBrokenSeconds);
            LadderSlowSeconds = Mathf.Max(0.5f, ladderSlowSeconds);
            LadderClimbDelay = Mathf.Max(0f, ladderClimbDelay);
            LadderRepairPresses = Mathf.Max(1, ladderRepairPresses);
            LadderToppledMaxSeconds = Mathf.Max(1f, ladderToppledMaxSeconds);
            CatOnLadderSeconds = Mathf.Max(0.5f, catOnLadderSeconds);
            IntroDelay = Mathf.Max(0f, introDelay);
            QuietFromLevel = quietFromLevel;
            QuietPoints = Mathf.Max(1, quietPoints);
            QuietGapMin = Mathf.Max(1, quietGapMin);
            QuietGapMax = Mathf.Max(QuietGapMin, quietGapMax);
            FrenzyFromLevel = frenzyFromLevel;
            FirstFrenzyDelay = Mathf.Max(0f, firstFrenzyDelay);
            FrenzySeconds = Mathf.Max(1f, frenzySeconds);
            FrenzyGap = Mathf.Max(1f, frenzyGap);
            FrenzyGapShrink = Mathf.Max(0f, frenzyGapShrink);
            FrenzyGapMin = Mathf.Max(1f, Mathf.Min(frenzyGapMin, FrenzyGap));
            FrenzyBreather = Mathf.Max(0f, frenzyBreather);
        }

        /// <summary>
        /// Level 1 below the first threshold, one more for each threshold reached, then one
        /// more every LevelEvery points past the last (review 2026-09-26: short early levels).
        /// </summary>
        public int LevelFor(int totalScore)
        {
            int score = Mathf.Max(0, totalScore);
            int level = 1;
            int last = 0;
            foreach (int threshold in LevelThresholds)
            {
                if (score < threshold)
                    return level;
                level++;
                last = threshold;
            }

            return level + (score - last) / LevelEvery;
        }

        /// <summary>
        /// Seconds between ladder / cat events: flat up to SpeedUpFromLevel, then shorter by
        /// EventGapShrink a level down to EventGapMin - the late game gets busier, never frantic.
        /// </summary>
        public float EventGapAt(int level) =>
            Mathf.Max(EventGapMin, EventGap - EventGapShrink * Mathf.Max(0, level - SpeedUpFromLevel));

        /// <summary>Seconds from the end of one terrible ten to the next: shorter every level past its first.</summary>
        public float FrenzyGapAt(int level) =>
            Mathf.Max(FrenzyGapMin, FrenzyGap - FrenzyGapShrink * Mathf.Max(0, level - FrenzyFromLevel));

        /// <summary>Seconds from the end of one rush hour to the next, shrinking the same way.</summary>
        public float RushGapAt(int level) =>
            Mathf.Max(RushGapMin, RushGap - RushGapShrink * Mathf.Max(0, level - SpeedUpFromLevel));
    }

    /// <summary>
    /// Decides when the café throws something at Miro. Pure: the game loop ticks it during live
    /// play only (not the breather, not the demo) and reports back when an event is over.
    /// One ladder/cat event at a time; a rush hour runs on its own clock and, below
    /// OverlapFromLevel, holds the smaller events back while it lasts.
    /// </summary>
    public sealed class ShiftEventDirector
    {
        static readonly ShiftEvent[] SmallEvents =
        {
            ShiftEvent.CatOnLadder, ShiftEvent.LadderCreak, ShiftEvent.LadderSlowClimb, ShiftEvent.LadderRepair
        };

        readonly EventSettings _settings;
        readonly IRandom _rng;
        readonly bool[] _seen = new bool[SmallEvents.Length];
        readonly ShiftEvent[] _pool = new ShiftEvent[SmallEvents.Length];
        float _untilEvent;
        float _untilRush;
        float _untilFrenzy;
        bool _frenzyDue;       // its clock ran out while the cat or Noir was busy: nothing new starts until it has had its turn
        int _lastLevel;

        public ShiftEventDirector(in EventSettings settings, IRandom rng)
        {
            _settings = settings;
            _rng = rng;
            Reset();
        }

        public void Reset()
        {
            _untilEvent = _settings.EventGap;
            _untilRush = _settings.FirstRushDelay;
            _untilFrenzy = _settings.FirstFrenzyDelay;
            _frenzyDue = false;
            _lastLevel = 0;
            Array.Clear(_seen, 0, _seen.Length);
        }

        /// <param name="smallEventActive">A ladder or the cat is still busy.</param>
        /// <param name="rushActive">A rush hour is running.</param>
        /// <param name="quiet">A quiet spell: nothing new starts and the clocks stand still.</param>
        /// <param name="baristaUp">Miro stands on a stool: only then can Noir shake one under him.</param>
        /// <param name="frenzyActive">A machine is going haywire: everything else waits for it.</param>
        public ShiftEvent Tick(float dt, int level, bool smallEventActive, bool rushActive, bool quiet = false,
            bool baristaUp = true, bool frenzyActive = false)
        {
            // A level that unlocks something new shows it soon, not a whole gap later
            // (review 2026-09-26: the ladders only turned up two levels after they unlocked).
            if (level > _lastLevel)
            {
                if (_lastLevel > 0 && Unlocks(_lastLevel, level))
                    _untilEvent = Mathf.Min(_untilEvent, _settings.IntroDelay);
                _lastLevel = level;
            }

            if (quiet || frenzyActive)
                return ShiftEvent.None;

            // The terrible ten seconds (level 5+): its clock runs outside rush hours; when it runs
            // out while the cat or Noir is busy it waits for them - and nothing new starts meanwhile -
            // so it never lands on top of anything, yet always comes.
            if (level >= _settings.FrenzyFromLevel && !rushActive)
            {
                if (!_frenzyDue)
                {
                    _untilFrenzy -= dt;
                    _frenzyDue = _untilFrenzy <= 0f;
                }

                if (_frenzyDue && !smallEventActive)
                {
                    _frenzyDue = false;
                    _untilFrenzy = _settings.FrenzyGapAt(level); // counted from its end, see FrenzyEnded
                    return ShiftEvent.MachineFrenzy;
                }
            }

            if (level >= _settings.RushFromLevel && !rushActive)
            {
                _untilRush -= dt;
                if (_untilRush <= 0f)
                {
                    _untilRush = _settings.RushGapAt(level); // counted from the rush's end, see RushEnded
                    return ShiftEvent.RushHour;
                }
            }

            if (level < _settings.CatFromLevel || smallEventActive)
                return ShiftEvent.None;
            if (_frenzyDue && !rushActive)
                return ShiftEvent.None;
            if (rushActive && level < _settings.OverlapFromLevel)
                return ShiftEvent.None;

            _untilEvent -= dt;
            if (_untilEvent > 0f)
                return ShiftEvent.None;

            _untilEvent = _settings.EventGapAt(level);
            return PickSmallEvent(level, baristaUp);
        }

        /// <summary>
        /// The next terrible ten seconds are counted from the end of these, and the cat and Noir
        /// give the player a breath first.
        /// </summary>
        public void FrenzyEnded(int level)
        {
            _untilFrenzy = _settings.FrenzyGapAt(level);
            _untilEvent = Mathf.Max(_untilEvent, _settings.FrenzyBreather);
        }

        /// <summary>The next rush hour is counted from the end of this one.</summary>
        public void RushEnded(int level) => _untilRush = _settings.RushGapAt(level);

        bool Unlocks(int from, int to) =>
            (from < _settings.CatFromLevel && to >= _settings.CatFromLevel)
            || (from < _settings.LaddersFromLevel && to >= _settings.LaddersFromLevel);

        /// <summary>
        /// The cat and the three ladder mishaps once unlocked: first the ones not seen yet this
        /// shift, so each one is met early, then all of them equally likely. The shaking stool
        /// needs Miro on a stool (review 2026-09-26), so it sits out while he is down.
        /// </summary>
        ShiftEvent PickSmallEvent(int level, bool baristaUp)
        {
            int unlocked = level >= _settings.LaddersFromLevel ? SmallEvents.Length : 1;
            int count = Fill(unlocked, baristaUp, unseenOnly: true);
            if (count == 0)
                count = Fill(unlocked, baristaUp, unseenOnly: false);

            ShiftEvent pick = _pool[_rng.NextInt(0, count)];
            _seen[Array.IndexOf(SmallEvents, pick)] = true;
            return pick;
        }

        int Fill(int unlocked, bool baristaUp, bool unseenOnly)
        {
            int count = 0;
            for (int i = 0; i < unlocked; i++)
            {
                if (unseenOnly && _seen[i])
                    continue;
                if (!baristaUp && SmallEvents[i] == ShiftEvent.LadderCreak)
                    continue;
                _pool[count++] = SmallEvents[i];
            }

            return count;
        }
    }

    /// <summary>What a quiet spell did this tick.</summary>
    public enum QuietChange
    {
        None,
        Started,
        Ended
    }

    /// <summary>
    /// From QuietFromLevel on the shift runs flat out, except that every so often - a random
    /// number of points apart - the café calms down for QuietPoints points: slower cups, one
    /// fewer on screen, no new events (review 2026-09-26). Counted in points, so a spell ends
    /// by playing through it. Never starts during a rush hour.
    /// </summary>
    public sealed class QuietSpellDirector
    {
        readonly EventSettings _settings;
        readonly IRandom _rng;
        int _nextAt = -1;
        int _endAt;

        public QuietSpellDirector(in EventSettings settings, IRandom rng)
        {
            _settings = settings;
            _rng = rng;
        }

        public bool Active { get; private set; }

        /// <summary>Points left in the running spell (0 when none).</summary>
        public int PointsLeft(int totalScore) => Active ? Mathf.Max(0, _endAt - totalScore) : 0;

        public void Reset()
        {
            Active = false;
            _nextAt = -1;
        }

        public QuietChange Tick(int totalScore, int level, bool rushActive)
        {
            if (Active)
            {
                if (totalScore < _endAt)
                    return QuietChange.None;

                Active = false;
                _nextAt = totalScore + Gap();
                return QuietChange.Ended;
            }

            if (level < _settings.QuietFromLevel)
                return QuietChange.None;

            if (_nextAt < 0)
                _nextAt = totalScore + Gap();

            if (rushActive || totalScore < _nextAt)
                return QuietChange.None;

            Active = true;
            _endAt = totalScore + _settings.QuietPoints;
            return QuietChange.Started;
        }

        int Gap() => _rng.NextInt(_settings.QuietGapMin, _settings.QuietGapMax + 1);
    }

    public enum LadderState
    {
        Standing,
        Creaking,
        /// <summary>Broke under Miro: nobody stands here and no cups come down this lane for a while.</summary>
        Broken,
        /// <summary>Askew: Miro gets up, but slowly.</summary>
        Wobbly,
        /// <summary>Knocked over: press its button a few times to set it back up.</summary>
        Toppled,
        CatOn
    }

    /// <summary>What a press on an upper lane's button does, given its ladder.</summary>
    public enum LadderPress
    {
        Move,
        Climb,
        Blocked,
        Shoo,
        RepairStep,
        Repaired
    }

    /// <summary>Something a ladder did on its own this frame.</summary>
    public enum LadderChange
    {
        None,
        Broke,
        Recovered,
        CatLeft
    }

    /// <summary>
    /// The two footstools under the upper barista slots (side 0 = left, 1 = right) and the
    /// mishaps that happen to them. Pure; the game loop moves Miro and draws the result.
    /// </summary>
    public sealed class LadderService
    {
        public const int Sides = 2;

        readonly EventSettings _settings;
        readonly LadderState[] _state = new LadderState[Sides];
        readonly float[] _timer = new float[Sides];
        readonly float[] _stood = new float[Sides];
        readonly int[] _repairsLeft = new int[Sides];

        public LadderService(in EventSettings settings)
        {
            _settings = settings;
        }

        public static int SideOf(LanePosition lane) => lane.IsLeft() ? 0 : 1;

        public static LanePosition UpperLane(int side) => side == 0 ? LanePosition.LeftUp : LanePosition.RightUp;

        public static LanePosition LowerLane(int side) => side == 0 ? LanePosition.LeftDown : LanePosition.RightDown;

        public LadderState State(int side) => _state[side];

        public int RepairsLeft(int side) => _repairsLeft[side];

        public float ClimbDelay => _settings.LadderClimbDelay;

        /// <summary>Anything but a plain standing ladder on either side.</summary>
        public bool AnyActive => _state[0] != LadderState.Standing || _state[1] != LadderState.Standing;

        /// <summary>Miro cannot stand on this side's upper slot.</summary>
        public bool Blocks(int side) => _state[side] is LadderState.Broken or LadderState.Toppled or LadderState.CatOn;

        /// <summary>A broken ladder's lane gets no new cups, so its break is not also a certain miss.</summary>
        public bool LaneClosed(int lane) =>
            (lane == (int)LanePosition.LeftUp && _state[0] == LadderState.Broken)
            || (lane == (int)LanePosition.RightUp && _state[1] == LadderState.Broken);

        public void Reset()
        {
            for (int side = 0; side < Sides; side++)
                SetState(side, LadderState.Standing, 0f);
        }

        /// <summary>
        /// Starts a ladder event and returns the side it hit, or -1 when it cannot start.
        /// `baristaUpSide` is the side whose upper slot Miro stands on (-1 when he is down):
        /// a creak picks his ladder when he is on one; the rest pick the other side, so a
        /// mishap never lands on him without warning.
        /// </summary>
        public int Start(ShiftEvent shiftEvent, int baristaUpSide, IRandom rng)
        {
            int side = PickSide(shiftEvent, baristaUpSide, rng);
            return StartOn(side, shiftEvent) ? side : -1;
        }

        /// <summary>Which stool a mishap is aimed at (see Start): Miro's for a shake, the other one otherwise.</summary>
        public static int PickSide(ShiftEvent shiftEvent, int baristaUpSide, IRandom rng) =>
            shiftEvent == ShiftEvent.LadderCreak && baristaUpSide >= 0
                ? baristaUpSide
                : baristaUpSide >= 0 ? 1 - baristaUpSide : rng.NextInt(0, Sides);

        /// <summary>
        /// What Noir's bump does when it lands a moment after the event started: with Miro up on
        /// that stool it shakes under him; a shake aimed at him when he has stepped off since
        /// only knocks it askew.
        /// </summary>
        public static ShiftEvent BumpOutcome(ShiftEvent planned, int side, int baristaUpSide)
        {
            if (planned == ShiftEvent.CatOnLadder)
                return planned;
            if (baristaUpSide == side)
                return ShiftEvent.LadderCreak;
            return planned == ShiftEvent.LadderCreak ? ShiftEvent.LadderSlowClimb : planned;
        }

        /// <summary>Starts a mishap on `side`; false when that stool is busy.</summary>
        public bool StartOn(int side, ShiftEvent shiftEvent)
        {
            if (side < 0 || side >= Sides || _state[side] != LadderState.Standing)
                return false;

            switch (shiftEvent)
            {
                case ShiftEvent.LadderCreak:
                    SetState(side, LadderState.Creaking, _settings.LadderCreakSeconds);
                    break;
                case ShiftEvent.LadderSlowClimb:
                    SetState(side, LadderState.Wobbly, _settings.LadderSlowSeconds);
                    break;
                case ShiftEvent.LadderRepair:
                    SetState(side, LadderState.Toppled, _settings.LadderToppledMaxSeconds);
                    _repairsLeft[side] = _settings.LadderRepairPresses;
                    break;
                case ShiftEvent.CatOnLadder:
                    SetState(side, LadderState.CatOn, _settings.CatOnLadderSeconds);
                    break;
                default:
                    return false;
            }

            return true;
        }

        /// <summary>Advances both ladders; `changes` receives what each side did on its own.</summary>
        public void Tick(float dt, int baristaUpSide, LadderChange[] changes)
        {
            for (int side = 0; side < Sides; side++)
            {
                changes[side] = LadderChange.None;
                LadderState state = _state[side];
                if (state == LadderState.Standing)
                    continue;

                if (state == LadderState.Creaking)
                {
                    _stood[side] = baristaUpSide == side ? _stood[side] + dt : 0f;
                    if (_stood[side] >= _settings.LadderBreakAfter)
                    {
                        SetState(side, LadderState.Broken, _settings.LadderBrokenSeconds);
                        changes[side] = LadderChange.Broke;
                        continue;
                    }
                }

                _timer[side] -= dt;
                if (_timer[side] > 0f)
                    continue;

                changes[side] = state == LadderState.CatOn ? LadderChange.CatLeft : LadderChange.Recovered;
                SetState(side, LadderState.Standing, 0f);
            }
        }

        /// <summary>A press on this side's upper button.</summary>
        public LadderPress Press(int side)
        {
            switch (_state[side])
            {
                case LadderState.Broken:
                    return LadderPress.Blocked;
                case LadderState.Wobbly:
                    return LadderPress.Climb;
                case LadderState.CatOn:
                    SetState(side, LadderState.Standing, 0f);
                    return LadderPress.Shoo;
                case LadderState.Toppled:
                    if (--_repairsLeft[side] > 0)
                        return LadderPress.RepairStep;
                    SetState(side, LadderState.Standing, 0f);
                    return LadderPress.Repaired;
                default:
                    return LadderPress.Move;
            }
        }

        void SetState(int side, LadderState state, float seconds)
        {
            _state[side] = state;
            _timer[side] = seconds;
            _stood[side] = 0f;
            if (state != LadderState.Toppled)
                _repairsLeft[side] = 0;
        }
    }
}
