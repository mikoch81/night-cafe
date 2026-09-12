using System.Collections.Generic;
using NightCafe.Config;
using NightCafe.Core;
using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    /// <summary>Scripted randomness: queued values, then the floor of the range.</summary>
    sealed class ScriptedRandom : IRandom
    {
        readonly Queue<float> _floats = new();
        readonly Queue<int> _ints = new();

        public ScriptedRandom Floats(params float[] values)
        {
            foreach (float value in values)
                _floats.Enqueue(value);

            return this;
        }

        public ScriptedRandom Ints(params int[] values)
        {
            foreach (int value in values)
                _ints.Enqueue(value);

            return this;
        }

        public float NextFloat() => _floats.Count > 0 ? _floats.Dequeue() : 0f;

        public int NextInt(int minInclusive, int maxExclusive) =>
            _ints.Count > 0 ? _ints.Dequeue() : minInclusive;
    }

    public sealed class OrderServiceTests
    {
        static OrderSettings ModeB(int colours = 4) => new(true, colours, 0.55f, 10, 20f);

        [Test]
        public void DisabledOrdersWantEverythingAndNeverRotate()
        {
            var orders = new OrderService(new OrderSettings(false, 4, 0.55f, 10, 20f), new ScriptedRandom());
            int changes = 0;
            orders.OrderChanged += _ => changes++;

            orders.Reset();
            for (int i = 0; i < 30; i++)
                orders.RegisterCorrectCatch();
            orders.Tick(100f);

            Assert.AreEqual(0, orders.CurrentOrder);
            Assert.IsTrue(orders.IsWanted(3));
            Assert.AreEqual(0, orders.RollCupColour());
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void ResetRollsAnOrderAndAnnouncesIt()
        {
            var orders = new OrderService(ModeB(), new ScriptedRandom().Ints(2));
            int announced = -1;
            orders.OrderChanged += c => announced = c;

            orders.Reset();

            Assert.AreEqual(2, orders.CurrentOrder);
            Assert.AreEqual(2, announced);
            Assert.IsTrue(orders.IsWanted(2));
            Assert.IsFalse(orders.IsWanted(0));
        }

        [Test]
        public void OrderRotatesAfterTenCorrectCatches()
        {
            var orders = new OrderService(ModeB(), new ScriptedRandom().Ints(0, 1));
            orders.Reset();
            var announced = new List<int>();
            orders.OrderChanged += announced.Add;

            for (int i = 0; i < 9; i++)
                orders.RegisterCorrectCatch();
            Assert.AreEqual(0, announced.Count, "nine catches keep the order");

            orders.RegisterCorrectCatch();

            Assert.AreEqual(1, announced.Count);
            Assert.AreEqual(1, orders.CurrentOrder, "offset 1 from colour 0");
            Assert.AreEqual(0, orders.CorrectCatches, "counter restarts with the new order");
        }

        [Test]
        public void OrderRotatesAfterTwentySeconds()
        {
            var orders = new OrderService(ModeB(), new ScriptedRandom().Ints(0, 3));
            orders.Reset();

            orders.Tick(19.9f);
            Assert.AreEqual(0, orders.CurrentOrder);

            orders.Tick(0.2f);
            Assert.AreEqual(3, orders.CurrentOrder);
            Assert.AreEqual(0f, orders.SecondsSinceChange, 0.0001f);
        }

        [Test]
        public void WhicheverComesFirstWins()
        {
            var orders = new OrderService(ModeB(), new ScriptedRandom().Ints(0, 1, 2));
            orders.Reset();

            for (int i = 0; i < 5; i++)
                orders.RegisterCorrectCatch();
            orders.Tick(20f);
            Assert.AreEqual(1, orders.CurrentOrder, "the timer fired first");
            Assert.AreEqual(0, orders.CorrectCatches, "and cleared the catch counter");

            orders.Tick(5f);
            for (int i = 0; i < 10; i++)
                orders.RegisterCorrectCatch();
            Assert.AreEqual(3, orders.CurrentOrder, "then the catches fired first (1 + 2)");
        }

        [Test]
        public void RotationNeverRepeatsTheSameColour()
        {
            // Offsets 1..3 from colour 2 must land on 3, 0, 1 - never on 2.
            foreach (int offset in new[] { 1, 2, 3 })
            {
                var orders = new OrderService(ModeB(), new ScriptedRandom().Ints(2, offset));
                orders.Reset();
                orders.Tick(20f);
                Assert.AreNotEqual(2, orders.CurrentOrder);
            }
        }

        [Test]
        public void CupColourFollowsTheGddWeights()
        {
            // Order = colour 1. Cumulative weights over [0,1,2,3] = 0.15, 0.70, 0.85, 1.00.
            var orders = new OrderService(ModeB(), new ScriptedRandom().Ints(1).Floats(0.10f, 0.20f, 0.69f, 0.80f, 0.99f));
            orders.Reset();

            Assert.AreEqual(0, orders.RollCupColour());
            Assert.AreEqual(1, orders.RollCupColour());
            Assert.AreEqual(1, orders.RollCupColour());
            Assert.AreEqual(2, orders.RollCupColour());
            Assert.AreEqual(3, orders.RollCupColour());
        }
    }

    public sealed class CatchRulesTests
    {
        [TestCase(true, true, CatchOutcome.Caught)]
        [TestCase(false, true, CatchOutcome.Missed)]
        [TestCase(true, false, CatchOutcome.WrongCatch)]
        [TestCase(false, false, CatchOutcome.Ignored)]
        public void OutcomeFollowsGdd3(bool present, bool wanted, CatchOutcome expected)
        {
            Assert.AreEqual(expected, CatchRules.Resolve(present, wanted));
        }

        [Test]
        public void OnlyMissAndWrongCatchCostAStain()
        {
            Assert.IsTrue(CatchRules.IsPenalised(CatchOutcome.Missed));
            Assert.IsTrue(CatchRules.IsPenalised(CatchOutcome.WrongCatch));
            Assert.IsFalse(CatchRules.IsPenalised(CatchOutcome.Caught));
            Assert.IsFalse(CatchRules.IsPenalised(CatchOutcome.Ignored));
        }
    }

    public sealed class ProfileServiceTests
    {
        [Test]
        public void FreshProfileHasNoRecordsAndTheWalnutShell()
        {
            var profile = new ProfileService(new InMemoryProfileStore());

            Assert.AreEqual(0, profile.Best(GameMode.A));
            Assert.AreEqual(0, profile.Best(GameMode.B));
            Assert.AreEqual(SkinCatalog.DefaultId, profile.SelectedSkin);
            Assert.AreEqual(GameMode.A, profile.SelectedMode);
        }

        [Test]
        public void SubmitRecordsPerModeAndReportsNewRecordsOnly()
        {
            var profile = new ProfileService(new InMemoryProfileStore());

            Assert.IsTrue(profile.SubmitScore(GameMode.A, 120));
            Assert.IsFalse(profile.SubmitScore(GameMode.A, 120), "equal is not a new record");
            Assert.IsFalse(profile.SubmitScore(GameMode.A, 50));
            Assert.IsTrue(profile.SubmitScore(GameMode.B, 10), "modes have separate records");

            Assert.AreEqual(120, profile.Best(GameMode.A));
            Assert.AreEqual(10, profile.Best(GameMode.B));
        }

        [Test]
        public void RecordsSurviveARoundTripThroughTheStore()
        {
            var store = new InMemoryProfileStore();
            var first = new ProfileService(store);
            first.SubmitScore(GameMode.B, 777);
            first.Unlock(SkinCatalog.OnyxId);
            first.SelectSkin(SkinCatalog.OnyxId);
            first.SelectMode(GameMode.B);

            var second = new ProfileService(store);

            Assert.AreEqual(777, second.Best(GameMode.B));
            Assert.IsTrue(second.IsUnlocked(SkinCatalog.OnyxId));
            Assert.AreEqual(SkinCatalog.OnyxId, second.SelectedSkin);
            Assert.AreEqual(GameMode.B, second.SelectedMode);
        }

        [Test]
        public void GarbageJsonStartsFreshInsteadOfThrowing()
        {
            var profile = new ProfileService(new InMemoryProfileStore("{not json"));
            Assert.AreEqual(0, profile.Best(GameMode.A));
        }

        [Test]
        public void ShortBestScoresArrayFromAnOlderBuildIsPadded()
        {
            var profile = new ProfileService(new InMemoryProfileStore("{\"bestScores\":[42]}"));

            Assert.AreEqual(42, profile.Best(GameMode.A));
            Assert.AreEqual(0, profile.Best(GameMode.B));
        }

        [Test]
        public void LockedSkinsCannotBeSelected()
        {
            var profile = new ProfileService(new InMemoryProfileStore());

            profile.SelectSkin(SkinCatalog.NeonId);

            Assert.AreEqual(SkinCatalog.DefaultId, profile.SelectedSkin);
        }

        [Test]
        public void UnlockReportsOnlyTheFirstTime()
        {
            var profile = new ProfileService(new InMemoryProfileStore());

            Assert.IsTrue(profile.Unlock(SkinCatalog.AshId));
            Assert.IsFalse(profile.Unlock(SkinCatalog.AshId));
            Assert.IsFalse(profile.Unlock(SkinCatalog.DefaultId), "walnut was never locked");
        }

        [Test]
        public void CyclingSkipsLockedSkinsAndWraps()
        {
            var profile = new ProfileService(new InMemoryProfileStore());
            profile.Unlock(SkinCatalog.NeonId);

            Assert.AreEqual(SkinCatalog.NeonId, profile.SelectNextSkin(), "ash and onyx are locked");
            Assert.AreEqual(SkinCatalog.DefaultId, profile.SelectNextSkin(), "wraps to walnut");
        }

        [Test]
        public void SelectedSkinFallsBackWhenTheSaveNamesAnUnknownOne()
        {
            var profile = new ProfileService(new InMemoryProfileStore(
                "{\"bestScores\":[0,0],\"unlockedSkins\":[\"chrome\"],\"selectedSkin\":\"chrome\"}"));

            Assert.AreEqual(SkinCatalog.DefaultId, profile.SelectedSkin);
        }
    }

    public sealed class SkinCatalogTests
    {
        readonly List<string> _result = new();

        [Test]
        public void ModeSkinUnlocksAtItsThreshold()
        {
            SkinCatalog.UnlockedBy(SkinCatalog.AshId, 250, 249, false, _result);
            CollectionAssert.IsEmpty(_result);

            SkinCatalog.UnlockedBy(SkinCatalog.AshId, 250, 250, false, _result);
            CollectionAssert.AreEqual(new[] { SkinCatalog.AshId }, _result);

            SkinCatalog.UnlockedBy(SkinCatalog.OnyxId, 500, 1050, false, _result);
            CollectionAssert.AreEqual(new[] { SkinCatalog.OnyxId }, _result, "uncapped total, not the wrapped counter");
        }

        [Test]
        public void ModesWithoutASkinUnlockNothing()
        {
            SkinCatalog.UnlockedBy("", 250, 999, false, _result);
            CollectionAssert.IsEmpty(_result);

            SkinCatalog.UnlockedBy(SkinCatalog.AshId, 0, 999, false, _result);
            CollectionAssert.IsEmpty(_result, "a zero threshold means no unlock, not a free one");
        }

        [Test]
        public void NeonNeedsARolloverWhateverTheMode()
        {
            SkinCatalog.UnlockedBy(SkinCatalog.AshId, 250, 1000, true, _result);
            CollectionAssert.AreEqual(new[] { SkinCatalog.AshId, SkinCatalog.NeonId }, _result);

            SkinCatalog.UnlockedBy(SkinCatalog.OnyxId, 500, 20, true, _result);
            CollectionAssert.AreEqual(new[] { SkinCatalog.NeonId }, _result);
        }

        [Test]
        public void EverySkinHasAUniqueIdAndWalnutIsFirst()
        {
            var ids = new HashSet<string>();
            foreach (Skin skin in SkinCatalog.All)
                Assert.IsTrue(ids.Add(skin.Id), $"duplicate skin id {skin.Id}");

            Assert.AreEqual(SkinCatalog.DefaultId, SkinCatalog.All[0].Id);
            Assert.IsFalse(SkinCatalog.TryGet("chrome", out _));
        }
    }

    public sealed class AttractPilotTests
    {
        const int CatchStep = 4;

        static AttractPilot Pilot(float reaction = 0f, float fumble = 0f, ScriptedRandom rng = null) =>
            new(rng ?? new ScriptedRandom().Floats(1f, 1f, 1f, 1f, 1f, 1f), reaction, fumble, CatchStep);

        static List<PilotCup> Cups(params PilotCup[] cups) => new(cups);

        [Test]
        public void StaysPutWithNothingInFlight()
        {
            Assert.AreEqual(-1, Pilot().Decide(Cups(), 0, 0.1f));
        }

        [Test]
        public void GoesToTheWantedCupClosestToTheBar()
        {
            var cups = Cups(new PilotCup(1, 2, 1, true), new PilotCup(2, 3, 3, true), new PilotCup(3, 1, 2, true));

            Assert.AreEqual(3, Pilot().Decide(cups, 0, 0.1f));
        }

        [Test]
        public void StaysWhenAlreadyUnderTheMostUrgentCup()
        {
            var cups = Cups(new PilotCup(1, 2, 1, true), new PilotCup(2, 3, 3, true));

            Assert.AreEqual(-1, Pilot().Decide(cups, 3, 0.1f));
        }

        [Test]
        public void IgnoresUnwantedColoursUnlessTheyThreatenTheTray()
        {
            var farAway = Cups(new PilotCup(1, 2, 1, false));
            Assert.AreEqual(-1, Pilot().Decide(farAway, 0, 0.1f), "an unwanted cup on another lane is nobody's problem");

            var landingOnMe = Cups(new PilotCup(1, 0, CatchStep - 1, false));
            Assert.AreEqual(1, Pilot().Decide(landingOnMe, 0, 0.1f), "steps aside to the first safe lane");
        }

        [Test]
        public void SteppingAsideAvoidsLanesWithTheirOwnThreat()
        {
            var cups = Cups(
                new PilotCup(1, 0, CatchStep - 1, false),
                new PilotCup(2, 1, CatchStep - 1, false));

            Assert.AreEqual(2, Pilot().Decide(cups, 0, 0.1f));
        }

        [Test]
        public void WaitsOutTheReactionTimeBeforeMoving()
        {
            AttractPilot pilot = Pilot(reaction: 0.25f);
            var cups = Cups(new PilotCup(1, 2, 3, true));

            Assert.AreEqual(-1, pilot.Decide(cups, 0, 0.1f));
            Assert.AreEqual(-1, pilot.Decide(cups, 0, 0.1f));
            Assert.AreEqual(2, pilot.Decide(cups, 0, 0.1f));
        }

        [Test]
        public void ANewTargetRestartsTheReactionClock()
        {
            AttractPilot pilot = Pilot(reaction: 0.2f);

            Assert.AreEqual(-1, pilot.Decide(Cups(new PilotCup(1, 2, 2, true)), 0, 0.15f));
            Assert.AreEqual(-1, pilot.Decide(Cups(new PilotCup(1, 2, 2, true), new PilotCup(2, 3, 3, true)), 0, 0.15f),
                "lane 3 became more urgent, so the clock restarts");
            Assert.AreEqual(3, pilot.Decide(Cups(new PilotCup(1, 2, 2, true), new PilotCup(2, 3, 3, true)), 0, 0.1f));
        }

        [Test]
        public void FumbledCupsAreLeftToDrop()
        {
            // First float roll (0.01) is below the 5 % fumble chance: cup 1 is written off.
            AttractPilot pilot = Pilot(fumble: 0.05f, rng: new ScriptedRandom().Floats(0.01f, 0.99f));
            var cups = Cups(new PilotCup(1, 2, 3, true), new PilotCup(2, 3, 1, true));

            Assert.AreEqual(3, pilot.Decide(cups, 0, 0.1f), "goes for cup 2 even though cup 1 is closer");
            Assert.AreEqual(3, pilot.Decide(cups, 0, 0.1f), "the fumble decision sticks for the cup's lifetime");
        }

        [Test]
        public void ForgetsCupsOnceTheyLand()
        {
            AttractPilot pilot = Pilot(fumble: 0.05f, rng: new ScriptedRandom().Floats(0.01f, 0.99f));

            pilot.Decide(Cups(new PilotCup(1, 2, 3, true)), 0, 0.1f); // written off
            pilot.Decide(Cups(), 0, 0.1f);                              // landed and forgotten

            Assert.AreEqual(2, pilot.Decide(Cups(new PilotCup(1, 2, 3, true)), 0, 0.1f),
                "a relaunched id gets a fresh roll (0.99 - not fumbled)");
        }
    }

    public sealed class SettingsGhostsTests
    {
        [Test]
        public void GhostsDefaultOffAndToggle()
        {
            var settings = new SettingsService(new InMemorySettingsStore());
            int changes = 0;
            settings.Changed += () => changes++;

            Assert.IsFalse(settings.GhostsEnabled);
            settings.ToggleGhosts();

            Assert.IsTrue(settings.GhostsEnabled);
            Assert.AreEqual(1, changes);
            Assert.IsTrue(settings.SfxEnabled && settings.MusicEnabled && settings.HapticsEnabled,
                "the ghost toggle leaves the other flags alone");
        }
    }
}
