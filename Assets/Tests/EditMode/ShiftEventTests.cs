using NightCafe.Config;
using NightCafe.Core;
using NightCafe.Services;
using NUnit.Framework;
using UnityEngine;

namespace NightCafe.Tests
{
    /// <summary>Levels, the café event director and the ladders (1.1.0, review 2026-09-26).</summary>
    public sealed class ShiftEventTests
    {
        sealed class FixedRandom : IRandom
        {
            public int Int;
            public float NextFloat() => 0f;
            public int NextInt(int minInclusive, int maxExclusive) => Mathf.Clamp(Int, minInclusive, maxExclusive - 1);
        }

        static EventSettings Settings()
        {
            var config = ScriptableObject.CreateInstance<ModeConfig>();
            try
            {
                return config.Events;
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void LevelsAreShortEarlyThenEveryFiftyPoints()
        {
            EventSettings s = Settings();
            Assert.AreEqual(1, s.LevelFor(0));
            Assert.AreEqual(1, s.LevelFor(24));
            Assert.AreEqual(2, s.LevelFor(25));
            Assert.AreEqual(2, s.LevelFor(49));
            Assert.AreEqual(3, s.LevelFor(50));
            Assert.AreEqual(4, s.LevelFor(100));
            Assert.AreEqual(4, s.LevelFor(139));
            Assert.AreEqual(5, s.LevelFor(140));
            Assert.AreEqual(5, s.LevelFor(189));
            Assert.AreEqual(6, s.LevelFor(190));
        }

        [Test]
        public void GapsShrinkUntilLevelFiveThenHoldAtAFloor()
        {
            EventSettings s = Settings();
            Assert.AreEqual(s.EventGapAt(2), s.EventGapAt(3), 1e-4f, "flat through the unlocks");
            Assert.Less(s.EventGapAt(4), s.EventGapAt(3), "busier from level 4");
            Assert.AreEqual(s.EventGapMin, s.EventGapAt(5), 1e-4f, "level 5 is flat out");
            Assert.AreEqual(s.EventGapMin, s.EventGapAt(40), 1e-4f);
            Assert.GreaterOrEqual(s.EventGapMin, 8f, "never frantic");
            Assert.AreEqual(s.RushGapMin, s.RushGapAt(5), 1e-4f);
            Assert.Greater(s.RushGapMin, s.RushDuration * 0.5f, "a rush hour is the exception, not the norm");
        }

        [Test]
        public void EventsUnlockByLevel()
        {
            EventSettings s = Settings();
            var rng = new FixedRandom { Int = 1 }; // the second of what is on offer
            var director = new ShiftEventDirector(s, rng);

            Assert.AreEqual(ShiftEvent.None, Run(director, 1, 120f), "level 1 is a plain shift");

            director.Reset();
            Assert.AreEqual(ShiftEvent.CatOnLadder, Run(director, 2, 120f), "level 2: only the cat");

            director.Reset();
            Assert.AreEqual(ShiftEvent.LadderCreak, Run(director, 3, 120f), "level 3: the ladders, no rush yet");

            director.Reset();
            Assert.AreEqual(ShiftEvent.RushHour, Run(director, 4, s.FirstRushDelay + 0.2f, busy: true),
                "level 4: the first rush hour comes quickly");

            director.Reset();
            Run(director, 4, s.FirstRushDelay - 1f, rushActive: true);
            Assert.AreEqual(ShiftEvent.None, Run(director, 4, 60f, rushActive: true),
                "below the overlap level a rush holds the small events");
        }

        [Test]
        public void AnUnlockShowsItsEventSoon()
        {
            EventSettings s = Settings();
            var director = new ShiftEventDirector(s, new FixedRandom { Int = 0 });
            Assert.AreEqual(ShiftEvent.None, Run(director, 1, 100f));
            Assert.AreEqual(ShiftEvent.CatOnLadder, Run(director, 2, s.IntroDelay + 0.2f), "the cat right after level 2");
            Assert.AreEqual(ShiftEvent.None, Run(director, 2, s.EventGapAt(2) - 1f));
            Assert.AreEqual(ShiftEvent.LadderCreak, Run(director, 3, s.IntroDelay + 0.2f), "a ladder right after level 3");
        }

        [Test]
        public void EveryMishapIsMetBeforeAnyRepeats()
        {
            EventSettings s = Settings();
            var director = new ShiftEventDirector(s, new FixedRandom { Int = 0 }); // always the first on offer
            var seen = new System.Collections.Generic.HashSet<ShiftEvent>();
            for (int i = 0; i < 4; i++)
                seen.Add(Run(director, 3, s.EventGap + 1f));
            Assert.AreEqual(4, seen.Count, "cat and all three ladders within the first four");
        }

        [Test]
        public void TheTerribleTenComesFromLevelFiveWhenTheCafeIsCalm()
        {
            EventSettings s = Settings();
            Assert.AreEqual(5, s.FrenzyFromLevel);
            Assert.GreaterOrEqual(s.FrenzySeconds, 8f);
            Assert.LessOrEqual(s.FrenzySeconds, 12f, "ten seconds, not a minute");

            var director = new ShiftEventDirector(s, new FixedRandom());
            ShiftEvent Tick(float dt, int level, bool busy = false, bool rush = false, bool frenzy = false) =>
                director.Tick(dt, level, busy, rush, false, true, frenzy);

            Tick(0.1f, 4); // level 4 first: no clock yet
            for (float t = 0f; t < 60f; t += 0.1f)
                Assert.AreNotEqual(ShiftEvent.MachineFrenzy, Tick(0.1f, 4, busy: true));

            director.Reset();
            for (float t = 0f; t < 100f; t += 0.1f)
                Assert.AreNotEqual(ShiftEvent.MachineFrenzy, Tick(0.1f, 5, busy: true), "never on top of the cat or a stool");

            // A calm café lets it through after the first delay.
            director.Reset();
            ShiftEvent e = ShiftEvent.None;
            for (float t = 0f; t < s.FirstFrenzyDelay + 0.2f && e != ShiftEvent.MachineFrenzy; t += 0.1f)
                e = Tick(0.1f, 6);
            Assert.AreEqual(ShiftEvent.MachineFrenzy, e);

            for (float t = 0f; t < 30f; t += 0.1f)
                Assert.AreEqual(ShiftEvent.None, Tick(0.1f, 6, frenzy: true), "while a machine is wild, nothing else starts");

            director.FrenzyEnded(6);
            Assert.AreNotEqual(ShiftEvent.MachineFrenzy, Tick(s.FrenzyGapAt(6) - 1f, 6), "the next one is a whole gap away");
            Assert.AreEqual(ShiftEvent.MachineFrenzy, Tick(2f, 6));
        }

        [Test]
        public void TheTerribleTenWaitsForTheCatThenComesAtOnce()
        {
            EventSettings s = Settings();
            var director = new ShiftEventDirector(s, new FixedRandom());
            ShiftEvent e = ShiftEvent.None;
            for (float t = 0f; t < s.FirstFrenzyDelay + 1f; t += 0.1f)
                e = director.Tick(0.1f, 5, true, false, false, true, false); // Noir is busy the whole time
            Assert.AreEqual(ShiftEvent.None, e, "due, but a stool is still in trouble");
            Assert.AreEqual(ShiftEvent.MachineFrenzy, director.Tick(0.1f, 5, false, false, false, true, false),
                "the moment it is clear");

            director.FrenzyEnded(5);
            for (float t = 0f; t < s.FrenzyBreather - 0.2f; t += 0.1f)
                Assert.AreEqual(ShiftEvent.None, director.Tick(0.1f, 5, false, true, false, true, false),
                    "a breath before the cat and Noir are back");
        }

        [Test]
        public void TheTerribleTenGetsWorseWithEveryLevel()
        {
            var config = ScriptableObject.CreateInstance<ModeConfig>();
            try
            {
                Assert.AreEqual(0.65f, config.FrenzyStepFactorAt(5), 1e-4f, "35 % faster at first");
                Assert.AreEqual(0.60f, config.FrenzyStepFactorAt(6), 1e-4f);
                Assert.AreEqual(0.50f, config.FrenzyStepFactorAt(8), 1e-4f, "twice the speed at most");
                Assert.AreEqual(0.50f, config.FrenzyStepFactorAt(30), 1e-4f);
                EventSettings s = config.Events;
                Assert.AreEqual(75f, s.FrenzyGapAt(5), 1e-4f);
                Assert.AreEqual(70f, s.FrenzyGapAt(6), 1e-4f);
                Assert.AreEqual(50f, s.FrenzyGapAt(30), 1e-4f, "never closer than this");
                Assert.Greater(s.FrenzyGapMin, s.FrenzySeconds * 4f, "the exception, not the shift");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ModeBHoldsItsOrderWhileAMachineRunsWild()
        {
            var config = ScriptableObject.CreateInstance<ModeConfig>();
            try
            {
                config.ordersEnabled = true;
                var orders = new OrderService(config.Orders, new FixedRandom { Int = 1 });
                orders.Reset();
                int order = orders.CurrentOrder;
                orders.Held = true;
                orders.Tick(config.orderChangeSeconds * 3f);
                for (int i = 0; i < config.orderChangeCatches * 2; i++)
                    orders.RegisterCorrectCatch();
                Assert.AreEqual(order, orders.CurrentOrder, "the stream never changes colour halfway");

                orders.Held = false;
                orders.Tick(0.01f);
                Assert.AreNotEqual(order, orders.CurrentOrder, "released, the catches made meanwhile count");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void NoirOnlyShakesAStoolWithMiroOnIt()
        {
            EventSettings s = Settings();
            var director = new ShiftEventDirector(s, new FixedRandom { Int = 1 }); // would be the shake
            for (int i = 0; i < 12; i++)
            {
                ShiftEvent e = ShiftEvent.None;
                for (float t = 0f; t < 60f && e == ShiftEvent.None; t += 0.1f)
                    e = director.Tick(0.1f, 3, false, false, baristaUp: false);
                Assert.AreNotEqual(ShiftEvent.LadderCreak, e, "Miro is down: no shaking stool");
                Assert.AreNotEqual(ShiftEvent.None, e);
            }
        }

        [Test]
        public void NoirsShoveDependsOnWhereMiroStandsWhenItLands()
        {
            Assert.AreEqual(ShiftEvent.LadderCreak, LadderService.BumpOutcome(ShiftEvent.LadderRepair, 1, 1),
                "he climbed onto it meanwhile: it shakes under him instead of tipping over with him");
            Assert.AreEqual(ShiftEvent.LadderSlowClimb, LadderService.BumpOutcome(ShiftEvent.LadderCreak, 0, -1),
                "the shake was aimed at him but he stepped down: the stool is only knocked askew");
            Assert.AreEqual(ShiftEvent.LadderRepair, LadderService.BumpOutcome(ShiftEvent.LadderRepair, 0, 1));
            Assert.AreEqual(ShiftEvent.LadderCreak, LadderService.BumpOutcome(ShiftEvent.LadderCreak, 1, 1));

            var ladders = new LadderService(Settings());
            Assert.IsTrue(ladders.StartOn(0, ShiftEvent.LadderRepair));
            Assert.IsFalse(ladders.StartOn(0, ShiftEvent.LadderSlowClimb), "one mishap per stool");
        }

        [Test]
        public void QuietSpellsComeFromLevelFiveForAFewPoints()
        {
            EventSettings s = Settings();
            var quiet = new QuietSpellDirector(s, new FixedRandom { Int = 0 }); // gaps at their minimum
            Assert.AreEqual(QuietChange.None, quiet.Tick(500, 4, false), "not before level 5");

            Assert.AreEqual(QuietChange.None, quiet.Tick(140, 5, false), "the first gap is counted from here");
            Assert.AreEqual(QuietChange.None, quiet.Tick(140 + s.QuietGapMin, 5, true), "never during a rush");
            Assert.AreEqual(QuietChange.Started, quiet.Tick(140 + s.QuietGapMin, 5, false));
            Assert.IsTrue(quiet.Active);
            Assert.AreEqual(s.QuietPoints, quiet.PointsLeft(140 + s.QuietGapMin));

            int end = 140 + s.QuietGapMin + s.QuietPoints;
            Assert.AreEqual(QuietChange.None, quiet.Tick(end - 1, 5, false));
            Assert.AreEqual(QuietChange.Ended, quiet.Tick(end, 5, false));
            Assert.AreEqual(QuietChange.None, quiet.Tick(end + s.QuietGapMin - 1, 6, false), "full speed in between");
            Assert.AreEqual(QuietChange.Started, quiet.Tick(end + s.QuietGapMin, 6, false));
        }

        [Test]
        public void AQuietSpellHoldsTheEventsBack()
        {
            EventSettings s = Settings();
            var director = new ShiftEventDirector(s, new FixedRandom());
            for (float t = 0f; t < 300f; t += 0.1f)
                Assert.AreEqual(ShiftEvent.None, director.Tick(0.1f, 6, false, false, quiet: true));
        }

        [Test]
        public void MiroHintsTheFirstTimesThenJokesWithoutRepeating()
        {
            var quips = new BaristaQuips(new FixedRandom { Int = 0 });
            string hint = quips.Line(Quip.LadderToppled);
            StringAssert.Contains("\n", hint);
            Assert.AreEqual(hint, quips.Line(Quip.LadderToppled), "a hint twice");
            string first = quips.Line(Quip.LadderToppled);
            Assert.AreNotEqual(hint, first, "then a joke");
            Assert.AreNotEqual(first, quips.Line(Quip.LadderToppled), "never the same joke twice in a row");

            Assert.IsFalse(BaristaQuips.HasHint(Quip.CatShooed));
            Assert.IsFalse(quips.Line(Quip.CatShooed).Contains("\n"), "a reaction is a joke straight away");

            quips.Reset();
            Assert.AreEqual(hint, quips.Line(Quip.LadderToppled), "a new shift brings the hints back");
            for (Quip q = 0; q < Quip.Count; q++)
                Assert.IsFalse(string.IsNullOrEmpty(quips.Line(q)), q.ToString());
        }

        [Test]
        public void OneSmallEventAtATimeAndTheGapCountsFromTheStart()
        {
            EventSettings s = Settings();
            var director = new ShiftEventDirector(s, new FixedRandom());
            Assert.AreEqual(ShiftEvent.None, director.Tick(s.EventGap - 0.1f, 2, false, false));
            Assert.AreEqual(ShiftEvent.CatOnLadder, director.Tick(0.2f, 2, false, false));
            Assert.AreEqual(ShiftEvent.None, director.Tick(1000f, 2, true, false), "busy: nothing new");
            Assert.AreEqual(ShiftEvent.CatOnLadder, director.Tick(s.EventGapAt(2), 2, false, false));
        }

        [Test]
        public void TheNextRushIsCountedFromTheEndOfTheLast()
        {
            EventSettings s = Settings();
            var director = new ShiftEventDirector(s, new FixedRandom());
            Assert.AreEqual(ShiftEvent.RushHour, director.Tick(s.FirstRushDelay + 0.01f, 4, true, false));
            Assert.AreEqual(ShiftEvent.None, director.Tick(1000f, 4, true, true), "no second rush during one");
            director.RushEnded(4);
            Assert.AreEqual(ShiftEvent.None, director.Tick(s.RushGapAt(4) - 0.1f, 4, true, false));
            Assert.AreEqual(ShiftEvent.RushHour, director.Tick(0.2f, 4, true, false));
        }

        [Test]
        public void ACreakingLadderBreaksOnlyUnderAMiroWhoStays()
        {
            EventSettings s = Settings();
            var ladders = new LadderService(s);
            var changes = new LadderChange[LadderService.Sides];

            Assert.AreEqual(0, ladders.Start(ShiftEvent.LadderCreak, 0, new FixedRandom()), "the creak picks his ladder");
            ladders.Tick(s.LadderBreakAfter * 0.6f, 0, changes);
            ladders.Tick(0.1f, -1, changes); // he stepped down: the count starts over
            ladders.Tick(s.LadderBreakAfter * 0.6f, 0, changes);
            Assert.AreEqual(LadderState.Creaking, ladders.State(0));

            ladders.Tick(s.LadderBreakAfter * 0.5f, 0, changes);
            Assert.AreEqual(LadderChange.Broke, changes[0]);
            Assert.AreEqual(LadderState.Broken, ladders.State(0));
            Assert.IsTrue(ladders.Blocks(0));
            Assert.IsTrue(ladders.LaneClosed((int)LanePosition.LeftUp), "no cups onto a broken ladder's lane");
            Assert.IsFalse(ladders.LaneClosed((int)LanePosition.LeftDown));
            Assert.AreEqual(LadderPress.Blocked, ladders.Press(0));

            ladders.Tick(s.LadderBrokenSeconds + 0.1f, -1, changes);
            Assert.AreEqual(LadderChange.Recovered, changes[0]);
            Assert.IsFalse(ladders.AnyActive);
        }

        [Test]
        public void ACreakThatNobodyStandsOnPassesQuietly()
        {
            EventSettings s = Settings();
            var ladders = new LadderService(s);
            var changes = new LadderChange[LadderService.Sides];
            int side = ladders.Start(ShiftEvent.LadderCreak, -1, new FixedRandom { Int = 1 });
            Assert.AreEqual(1, side);
            ladders.Tick(s.LadderCreakSeconds + 0.1f, -1, changes);
            Assert.AreEqual(LadderChange.Recovered, changes[1]);
            Assert.AreEqual(LadderState.Standing, ladders.State(1));
        }

        [Test]
        public void MishapsLandOnTheOtherLadderThanMiros()
        {
            var ladders = new LadderService(Settings());
            Assert.AreEqual(1, ladders.Start(ShiftEvent.CatOnLadder, 0, new FixedRandom()));
            Assert.AreEqual(0, ladders.Start(ShiftEvent.LadderRepair, 1, new FixedRandom()));
            Assert.AreEqual(-1, ladders.Start(ShiftEvent.LadderSlowClimb, 0, new FixedRandom()),
                "the other side is taken: nothing starts");
        }

        [Test]
        public void TheCatIsShooedByOnePressAndTheStoolSetUpByTwo()
        {
            EventSettings s = Settings();
            var ladders = new LadderService(s);
            ladders.Start(ShiftEvent.CatOnLadder, 1, new FixedRandom());
            Assert.IsTrue(ladders.Blocks(0));
            Assert.AreEqual(LadderPress.Shoo, ladders.Press(0));
            Assert.AreEqual(LadderPress.Move, ladders.Press(0), "the next press climbs up");

            ladders.Start(ShiftEvent.LadderRepair, 1, new FixedRandom());
            Assert.AreEqual(s.LadderRepairPresses, ladders.RepairsLeft(0));
            Assert.AreEqual(LadderPress.RepairStep, ladders.Press(0));
            Assert.AreEqual(LadderPress.Repaired, ladders.Press(0));
            Assert.AreEqual(LadderState.Standing, ladders.State(0));
            Assert.IsFalse(ladders.LaneClosed((int)LanePosition.LeftUp), "a knocked-over stool keeps its lane open");
        }

        [Test]
        public void AnAskewLadderMakesMiroClimbUntilItSettles()
        {
            EventSettings s = Settings();
            var ladders = new LadderService(s);
            var changes = new LadderChange[LadderService.Sides];
            ladders.Start(ShiftEvent.LadderSlowClimb, -1, new FixedRandom());
            Assert.IsFalse(ladders.Blocks(0), "he can stand there, it just takes a moment");
            Assert.AreEqual(LadderPress.Climb, ladders.Press(0));
            Assert.Greater(ladders.ClimbDelay, 0.2f);
            Assert.Less(ladders.ClimbDelay, 0.8f, "slower, still catchable with a look ahead");
            ladders.Tick(s.LadderSlowSeconds + 0.1f, -1, changes);
            Assert.AreEqual(LadderPress.Move, ladders.Press(0));
        }

        [Test]
        public void RushHourMakesCupsFasterEvenAtTheTempoFloor()
        {
            var config = ScriptableObject.CreateInstance<ModeConfig>();
            try
            {
                var tempo = new TempoService(config.Tempo);
                for (int i = 0; i < 500; i++)
                    tempo.RegisterCatch();
                float floor = tempo.StepTime;
                tempo.StepFactor = config.rushStepFactor;
                Assert.Less(tempo.StepTime, floor);
                Assert.Greater(tempo.StepTime, 0.3f, "rush on top of T9 stays playable");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        /// <summary>Ticks until something starts; `busy` keeps a small event running (only a rush can start).</summary>
        static ShiftEvent Run(ShiftEventDirector director, int level, float seconds, bool rushActive = false,
            bool busy = false)
        {
            for (float t = 0f; t < seconds; t += 0.1f)
            {
                ShiftEvent e = director.Tick(0.1f, level, busy, rushActive);
                if (e != ShiftEvent.None)
                    return e;
            }

            return ShiftEvent.None;
        }
    }
}
