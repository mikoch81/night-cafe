using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    public sealed class DeviceLayoutTests
    {
        const float Tolerance = 0.001f;

        [Test]
        public void FillsWidthOnTheTestPhone()
        {
            // 2424x1080
            Assert.AreEqual(4.188f, DeviceLayout.OrthographicSizeFor(2424f / 1080f), Tolerance);
        }

        [Test]
        public void ClampsSoTheCameraNeverLooksPastTheWood()
        {
            Assert.AreEqual(DeviceLayout.ShellContentHalfHeight,
                DeviceLayout.OrthographicSizeFor(16f / 9f), Tolerance, "16:9 would need 5.29");

            Assert.AreEqual(DeviceLayout.ShellContentHalfHeight,
                DeviceLayout.OrthographicSizeFor(1.60f), Tolerance, "4:3-ish would need 5.88");
        }

        [Test]
        public void ClampsSoTheLcdTopStaysOnScreenOnVeryWidePhones()
        {
            Assert.AreEqual(4.10f, DeviceLayout.OrthographicSizeFor(2.40f), Tolerance);
            Assert.AreEqual(4.10f, DeviceLayout.OrthographicSizeFor(3.00f), Tolerance);
        }

        [Test]
        public void LcdStaysFullyVisibleAcrossEveryPlausibleAspect()
        {
            for (float aspect = 1.30f; aspect <= 3.00f; aspect += 0.01f)
                Assert.IsTrue(DeviceLayout.LcdFullyVisible(aspect), $"cropped at aspect {aspect:0.00}");
        }

        [Test]
        public void ScreenScaleFitsTheCutoutByHeight()
        {
            // screen_bg is 892 px tall, the cutout 712 px.
            Assert.AreEqual(712f / 892f, DeviceLayout.ScreenScale, 0.0001f);
        }

        [Test]
        public void ScaledScreenFitsInsideTheCutoutWidth()
        {
            const float screenBgWidth = 12.72f;
            const float cutoutWidth = 10.28f;

            Assert.LessOrEqual(screenBgWidth * DeviceLayout.ScreenScale, cutoutWidth + 0.001f,
                "fitting by height must not overflow the cutout horizontally");
        }

        [Test]
        public void ScreenOffsetPutsTheGlassOverTheCutout()
        {
            // Cutout centre is at canvas y 492 of 1080, i.e. 0.48 world units above centre.
            Assert.AreEqual(0.48f, DeviceLayout.ScreenOffsetY, 0.0001f);
        }
    }
}
