using NightCafe.Services;
using NUnit.Framework;

namespace NightCafe.Tests
{
    public sealed class DeviceLayoutTests
    {
        const float Tolerance = 0.001f;

        [Test]
        public void HeightGovernsOnThePhone()
        {
            // 2424x1080: the device is 20:9, so its height plus the margin sets the view.
            Assert.AreEqual(5.05f, DeviceLayout.OrthographicSizeFor(2424f / 1080f), Tolerance);
            Assert.AreEqual(5.05f, DeviceLayout.OrthographicSizeFor(3.00f), Tolerance);
        }

        [Test]
        public void WidthGovernsOnTallerScreens()
        {
            Assert.AreEqual(11.25f / (16f / 9f), DeviceLayout.OrthographicSizeFor(16f / 9f), Tolerance);
            Assert.AreEqual(11.25f / 1.60f, DeviceLayout.OrthographicSizeFor(1.60f), Tolerance);
        }

        [Test]
        public void WholeDeviceStaysVisibleAcrossEveryPlausibleAspect()
        {
            for (float aspect = 1.30f; aspect <= 3.00f; aspect += 0.01f)
                Assert.IsTrue(DeviceLayout.DeviceFullyVisible(aspect), $"cropped at aspect {aspect:0.00}");
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
            // Cutout centre is 35 px above the canvas centre (tools/shell_render.py LCD_CENTRE_Y).
            Assert.AreEqual(0.35f, DeviceLayout.ScreenOffsetY, 0.0001f);
        }
    }
}
