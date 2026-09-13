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
        public void SeenStraightOnTheMarginIsExact()
        {
            // With no tilt the slab is a 2:1 rectangle whose top face is half a thickness nearer
            // than the target, and the classic framing applies: height plus margin on the phone,
            // width plus margin at 16:9.
            float phone = (DeviceLayout.BodyHalfHeight + DeviceLayout.Margin) / HalfTan + DeviceLayout.BodyThickness * 0.5f;
            Assert.AreEqual(phone, DeviceLayout.CameraDistanceFor(2424f / 1080f, Fov, 0f), Tolerance);
            Assert.AreEqual(phone, DeviceLayout.CameraDistanceFor(3.0f, Fov, 0f), Tolerance);

            float aspect = 16f / 9f;
            float editor = (DeviceLayout.BodyHalfWidth + DeviceLayout.Margin) / (HalfTan * aspect) + DeviceLayout.BodyThickness * 0.5f;
            Assert.AreEqual(editor, DeviceLayout.CameraDistanceFor(aspect, Fov, 0f), Tolerance);
        }

        [Test]
        public void HeightGovernsOnThePhoneAndWidthOnTallerScreens()
        {
            float phone = DeviceLayout.CameraDistanceFor(2424f / 1080f, Fov);
            Assert.AreEqual(phone, DeviceLayout.CameraDistanceFor(3.0f, Fov), Tolerance, "wider screens keep the phone distance");
            float editor = DeviceLayout.CameraDistanceFor(16f / 9f, Fov);
            Assert.Greater(editor, phone);
            Assert.Greater(DeviceLayout.CameraDistanceFor(1.6f, Fov), editor, "a squarer screen needs more distance");
        }

        [Test]
        public void TiltingBacksTheCameraOffJustEnough()
        {
            // Tilting trades one critical corner for another (top face corners straight on, the
            // bottom face's near edge tilted); the distance barely moves and nothing is cropped.
            float straight = DeviceLayout.CameraDistanceFor(2424f / 1080f, Fov, 0f);
            float tilted = DeviceLayout.CameraDistanceFor(2424f / 1080f, Fov, DeviceLayout.CameraTiltDegrees);
            Assert.IsTrue(DeviceLayout.DeviceFullyVisible(2424f / 1080f, Fov));
            Assert.Less(Mathf.Abs(tilted - straight), 1.0f, "the tilt is not a zoom");
        }

        [Test]
        public void CameraSitsOnThePlayersSideInFrontOfTheTopFace()
        {
            Vector3 offset = DeviceLayout.CameraOffset(DeviceLayout.CameraTiltDegrees, 10f);
            Assert.Less(offset.y, 0f, "near side = the bottom edge of the screen");
            Assert.Less(offset.z, 0f, "the top face is at negative z");
            Assert.AreEqual(10f, offset.magnitude, Tolerance);
        }

        [Test]
        public void WholeDeviceStaysVisibleAcrossEveryPlausibleAspectAndTilt()
        {
            for (float tilt = 0f; tilt <= 20f; tilt += 2f)
                for (float aspect = 1.30f; aspect <= 3.00f; aspect += 0.01f)
                    Assert.IsTrue(DeviceLayout.DeviceFullyVisible(aspect, Fov, tilt), $"cropped at aspect {aspect:0.00}, tilt {tilt}");
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
