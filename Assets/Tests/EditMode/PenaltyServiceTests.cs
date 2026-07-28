using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    public sealed class PenaltyServiceTests
    {
        [Test]
        public void GameOverTriggersExactlyAtMaxStains()
        {
            var service = new PenaltyService(3);
            int gameOvers = 0;
            service.GameOverTriggered += () => gameOvers++;

            service.AddStain();
            service.AddStain();
            Assert.AreEqual(0, gameOvers, "two stains must not end the shift");

            service.AddStain();
            Assert.AreEqual(1, gameOvers);
            Assert.AreEqual(3, service.Stains);
            Assert.IsTrue(service.IsGameOver);
        }

        [Test]
        public void AddStainBeyondMaxIsIgnored()
        {
            var service = new PenaltyService(3);
            int gameOvers = 0;
            service.GameOverTriggered += () => gameOvers++;

            for (int i = 0; i < 6; i++)
                service.AddStain();

            Assert.AreEqual(3, service.Stains);
            Assert.AreEqual(1, gameOvers, "game over must fire only once");
        }

        [Test]
        public void TryRemoveStainWipesOneStain()
        {
            var service = new PenaltyService(3);
            service.AddStain();
            service.AddStain();

            Assert.IsTrue(service.TryRemoveStain());
            Assert.AreEqual(1, service.Stains);
        }

        [Test]
        public void TryRemoveStainOnCleanBoardReturnsFalse()
        {
            var service = new PenaltyService(3);

            Assert.IsFalse(service.TryRemoveStain());
            Assert.AreEqual(0, service.Stains);
        }

        [Test]
        public void StainsChangedReportsCurrentCount()
        {
            var service = new PenaltyService(3);
            int last = -1;
            service.StainsChanged += count => last = count;

            service.AddStain();
            Assert.AreEqual(1, last);

            service.TryRemoveStain();
            Assert.AreEqual(0, last);
        }

        [Test]
        public void ResetClearsStains()
        {
            var service = new PenaltyService(3);
            service.AddStain();
            service.AddStain();

            service.Reset();

            Assert.AreEqual(0, service.Stains);
            Assert.IsFalse(service.IsGameOver);
        }
    }
}
