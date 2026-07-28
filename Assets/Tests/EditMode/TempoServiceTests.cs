using NightCafe.Config;
using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    public sealed class TempoServiceTests
    {
        const float Tolerance = 0.0001f;

        static TempoSettings DefaultSettings() =>
            new(baseStepTime: 0.90f, decrement: 0.055f, minStepTime: 0.40f,
                catchesPerLevel: 12, maxLevel: 9, startLevel: 0);

        static TempoService Catch(TempoService service, int times)
        {
            for (int i = 0; i < times; i++)
                service.RegisterCatch();

            return service;
        }

        [Test]
        public void StartsAtLevelZeroWithBaseStepTime()
        {
            var service = new TempoService(DefaultSettings());

            Assert.AreEqual(0, service.Level);
            Assert.AreEqual(0.90f, service.StepTime, Tolerance);
        }

        [Test]
        public void LevelIncreasesEveryTwelveCatches()
        {
            var service = new TempoService(DefaultSettings());

            Catch(service, 11);
            Assert.AreEqual(0, service.Level, "level must not rise before the 12th catch");

            service.RegisterCatch();
            Assert.AreEqual(1, service.Level);

            Catch(service, 12);
            Assert.AreEqual(2, service.Level);
        }

        [Test]
        public void StepTimeMatchesGddFormula()
        {
            var service = new TempoService(DefaultSettings());

            Catch(service, 5 * 12);
            Assert.AreEqual(5, service.Level);
            Assert.AreEqual(0.625f, service.StepTime, Tolerance, "0.90 - 0.055 * 5");

            Catch(service, 4 * 12);
            Assert.AreEqual(9, service.Level);
            Assert.AreEqual(0.405f, service.StepTime, Tolerance, "0.90 - 0.055 * 9");
        }

        [Test]
        public void LevelIsClampedAtMax()
        {
            var service = new TempoService(DefaultSettings());

            Catch(service, 40 * 12);

            Assert.AreEqual(9, service.Level);
            Assert.AreEqual(0.405f, service.StepTime, Tolerance);
        }

        [Test]
        public void StepTimeNeverGoesBelowFloor()
        {
            var settings = new TempoSettings(0.90f, 0.20f, 0.40f, 12, 9, 0);
            var service = new TempoService(settings);

            Catch(service, 5 * 12);

            Assert.AreEqual(0.40f, service.StepTime, Tolerance, "0.90 - 0.20 * 5 = -0.10 must clamp to the floor");
        }

        [Test]
        public void LevelChangedFiresOncePerLevel()
        {
            var service = new TempoService(DefaultSettings());
            int events = 0;
            service.LevelChanged += _ => events++;

            Catch(service, 24);

            Assert.AreEqual(2, events);
        }

        [Test]
        public void ResetReturnsToStartLevel()
        {
            var service = new TempoService(DefaultSettings());
            Catch(service, 36);

            service.Reset();

            Assert.AreEqual(0, service.Level);
            Assert.AreEqual(0, service.Catches);
            Assert.AreEqual(0.90f, service.StepTime, Tolerance);
        }

        [Test]
        public void StartLevelOffsetsProgression()
        {
            var settings = new TempoSettings(0.90f, 0.055f, 0.40f, 10, 9, 2);
            var service = new TempoService(settings);

            Assert.AreEqual(2, service.Level, "Mode B starts at T2");

            Catch(service, 10);
            Assert.AreEqual(3, service.Level);
        }
    }
}
