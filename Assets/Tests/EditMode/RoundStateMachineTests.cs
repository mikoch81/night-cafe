using NightCafe.Core;
using NUnit.Framework;

namespace NightCafe.Tests
{
    public sealed class RoundStateMachineTests
    {
        const float AttractDelay = 8f;
        const float DemoMax = 45f;
        const float GameOverIdle = 6f;
        const float Lockout = 0.8f;
        const float Breather = 2.5f;

        static RoundStateMachine Machine()
        {
            var machine = new RoundStateMachine(new RoundTimings(AttractDelay, DemoMax, GameOverIdle, Lockout));
            machine.EnteredTitle(100f);
            return machine;
        }

        [Test]
        public void StartsOnTheTitleWithTheUiLive()
        {
            RoundStateMachine machine = Machine();

            Assert.AreEqual(GameState.Title, machine.State);
            Assert.IsFalse(machine.IsDemo);
            Assert.IsTrue(machine.TitleUiActive);
            Assert.IsFalse(machine.AcceptsLaneMoves);
        }

        [Test]
        public void IdleTitleTurnsIntoTheDemoAfterTheAttractDelay()
        {
            RoundStateMachine machine = Machine();

            Assert.AreEqual(RoundTransition.None, machine.Tick(107.9f));
            Assert.AreEqual(RoundTransition.EnterDemo, machine.Tick(108f));
        }

        [Test]
        public void ATitlePressOnAWidgetOnlyResetsTheIdleClock()
        {
            RoundStateMachine machine = Machine();

            Assert.AreEqual(RoundTransition.None, machine.Press(105f, consumedByTitleUi: true));
            Assert.AreEqual(RoundTransition.None, machine.Tick(112.9f), "idle clock restarted at 105");
            Assert.AreEqual(RoundTransition.EnterDemo, machine.Tick(113f));
        }

        [Test]
        public void ATitlePressElsewhereStartsARound()
        {
            RoundStateMachine machine = Machine();

            Assert.AreEqual(RoundTransition.StartRound, machine.Press(101f, consumedByTitleUi: false));
            machine.RoundStarted();

            Assert.AreEqual(GameState.Playing, machine.State);
            Assert.IsTrue(machine.AcceptsLaneMoves);
            Assert.IsFalse(machine.TitleUiActive);
            Assert.AreEqual(RoundTransition.None, machine.Press(102f, false), "presses in play are lane moves, not transitions");
        }

        [Test]
        public void DemoIsPlayingButIgnoresLaneMovesAndTitleWidgets()
        {
            RoundStateMachine machine = Machine();
            machine.EnteredDemo(108f);

            Assert.AreEqual(GameState.Playing, machine.State);
            Assert.IsTrue(machine.IsDemo);
            Assert.IsFalse(machine.AcceptsLaneMoves);
            Assert.IsFalse(machine.TitleUiActive);
        }

        [Test]
        public void AnyPressDuringTheDemoStartsARealRound()
        {
            RoundStateMachine machine = Machine();
            machine.EnteredDemo(108f);

            Assert.AreEqual(RoundTransition.StartRound, machine.Press(110f, consumedByTitleUi: false));
        }

        [Test]
        public void FlippingTheLeverDuringTheDemoHandsBackTheTitleInsteadOfStartingARound()
        {
            RoundStateMachine machine = Machine();
            machine.EnteredDemo(108f);

            Assert.AreEqual(RoundTransition.EnterTitle, machine.Press(110f, consumedByTitleUi: true),
                "the lever is real hardware under the finger even while the pilot plays");
        }

        [Test]
        public void DemoTimesOutBackToTheTitle()
        {
            RoundStateMachine machine = Machine();
            machine.EnteredDemo(108f);

            Assert.AreEqual(RoundTransition.None, machine.Tick(152.9f));
            Assert.AreEqual(RoundTransition.EnterTitle, machine.Tick(153f));
        }

        [Test]
        public void ThirdStainEndsARoundButOnlyHandsBackTheTitleInADemo()
        {
            RoundStateMachine live = Machine();
            live.RoundStarted();
            Assert.AreEqual(RoundTransition.EnterGameOver, live.PenaltyGameOver());

            RoundStateMachine demo = Machine();
            demo.EnteredDemo(108f);
            Assert.AreEqual(RoundTransition.EnterTitle, demo.PenaltyGameOver());
        }

        [Test]
        public void GameOverIgnoresPressesDuringTheLockoutThenRestarts()
        {
            RoundStateMachine machine = Machine();
            machine.RoundStarted();
            machine.EnteredGameOver(200f);

            Assert.AreEqual(RoundTransition.None, machine.Press(200.5f, false), "tap already in flight");
            Assert.AreEqual(RoundTransition.StartRound, machine.Press(200.8f, false));
        }

        [Test]
        public void UntouchedGameOverReturnsToTheTitle()
        {
            RoundStateMachine machine = Machine();
            machine.RoundStarted();
            machine.EnteredGameOver(200f);

            Assert.AreEqual(RoundTransition.None, machine.Tick(205.9f));
            Assert.AreEqual(RoundTransition.EnterTitle, machine.Tick(206f));
        }

        [Test]
        public void BreatherOnlyInterruptsLivePlayAndResumesOnTime()
        {
            RoundStateMachine machine = Machine();
            Assert.IsFalse(machine.TryStartBreather(100f, Breather), "not on the title");

            machine.RoundStarted();
            Assert.IsTrue(machine.TryStartBreather(120f, Breather));
            Assert.AreEqual(GameState.Breather, machine.State);
            Assert.IsTrue(machine.AcceptsLaneMoves, "the barista may still reposition while cups finish");
            Assert.IsFalse(machine.TryStartBreather(121f, Breather), "a second breather during the first is dropped");

            Assert.AreEqual(RoundTransition.None, machine.Tick(122.4f));
            Assert.AreEqual(RoundTransition.ResumePlaying, machine.Tick(122.5f));
            machine.ResumedPlaying();
            Assert.AreEqual(GameState.Playing, machine.State);
        }

        [Test]
        public void DemoTimeoutWinsEvenInsideABreather()
        {
            RoundStateMachine machine = Machine();
            machine.EnteredDemo(108f);
            machine.TryStartBreather(152f, Breather);

            Assert.AreEqual(RoundTransition.EnterTitle, machine.Tick(153f));
        }
    }
}
