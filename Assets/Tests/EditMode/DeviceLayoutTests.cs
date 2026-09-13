using NightCafe.Services;
using NUnit.Framework;
using UnityEngine;

namespace NightCafe.Tests
{
    public sealed class DeviceLayoutTests
    {
        const float Fov = 24f;
        const float Tolerance = 0.001f;

        static float HalfTan => Mathf.Tan(0.5f * Fov * Mathf.Deg2Rad);

        [Test]
        public void HeightGovernsOnThePhone()
        {
            // 2424x1080: the body is 2:1, so its height plus the margin sets the distance.
            float expected = (DeviceLayout.BodyHalfHeight + DeviceLayout.Margin) / HalfTan;
            Assert.AreEqual(expected, DeviceLayout.CameraDistanceFor(2424f / 1080f, Fov), Tolerance);
            Assert.AreEqual(expected, DeviceLayout.CameraDistanceFor(3.0f, Fov), Tolerance);
        }

        [Test]
        public void WidthGovernsOnTallerScreens()
        {
            float aspect = 16f / 9f;
            float expected = (DeviceLayout.BodyHalfWidth + DeviceLayout.Margin) / (HalfTan * aspect);
            Assert.AreEqual(expected, DeviceLayout.CameraDistanceFor(aspect, Fov), Tolerance);
            Assert.Greater(DeviceLayout.CameraDistanceFor(1.6f, Fov), expected, "a squarer screen needs more distance");
        }

        [Test]
        public void WholeDeviceStaysVisibleAcrossEveryPlausibleAspect()
        {
            for (float aspect = 1.30f; aspect <= 3.00f; aspect += 0.01f)
                Assert.IsTrue(DeviceLayout.DeviceFullyVisible(aspect, Fov), $"cropped at aspect {aspect:0.00}");
        }

        [Test]
        public void LcdCameraShowsScreenBgOneToOne()
        {
            // screen_bg.png is 1272x892 px at 100 PPU; the LCD face keeps the same proportion.
            Assert.AreEqual(8.92f / 2f, DeviceLayout.LcdOrthographicSize, 0.0001f);
            Assert.AreEqual(1272f / 892f, DeviceLayout.LcdWidth / DeviceLayout.LcdHeight, 0.002f);
        }
    }
}
