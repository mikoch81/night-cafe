using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    public sealed class BrewTimerTests
    {
        [Test]
        public void CyclesThroughTheMinutesAndBackToOff()
        {
            var timer = new BrewTimer(3);
            Assert.AreEqual(0, timer.Minutes);
            Assert.IsFalse(timer.IsRunning);

            timer.Cycle(0); Assert.AreEqual(1, timer.Minutes);
            timer.Cycle(0); Assert.AreEqual(2, timer.Minutes);
            timer.Cycle(0); Assert.AreEqual(3, timer.Minutes);
            Assert.IsTrue(timer.IsRunning);
            timer.Cycle(0);
            Assert.AreEqual(0, timer.Minutes);
            Assert.IsFalse(timer.IsRunning);
        }

        [Test]
        public void EveryStepRestartsTheCountdown()
        {
            var timer = new BrewTimer(5);
            timer.Cycle(100);
            Assert.AreEqual(60.0, timer.Remaining(100), 0.001);
            timer.Cycle(130); // 2 minutes from now, not 1:30 + 60
            Assert.AreEqual(120.0, timer.Remaining(130), 0.001);
        }

        [Test]
        public void TickFiresOnceWhenTheTimeIsUp()
        {
            var timer = new BrewTimer(5);
            timer.Cycle(0);
            Assert.IsFalse(timer.Tick(59.9));
            Assert.IsTrue(timer.Tick(60.0));
            Assert.IsFalse(timer.Tick(61.0), "the alarm must not repeat");
            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(0, timer.Minutes);
        }

        [Test]
        public void FormatsRemainingSecondsAsMinutesAndSeconds()
        {
            Assert.AreEqual("05:00", BrewTimer.Format(300.0, true));
            Assert.AreEqual("05 00", BrewTimer.Format(299.2, false), "ceil: 5:00 stays up for the first second");
            Assert.AreEqual("04:59", BrewTimer.Format(298.9, true));
            Assert.AreEqual("00:00", BrewTimer.Format(0.0, true));
        }
    }
}
