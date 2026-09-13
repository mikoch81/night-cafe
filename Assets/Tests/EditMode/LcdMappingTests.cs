using NightCafe.Services;
using NUnit.Framework;
using UnityEngine;

namespace NightCafe.Tests
{
    public sealed class LcdMappingTests
    {
        [Test]
        public void CentreOfTheFaceIsTheCentreOfTheLcdScene()
        {
            Vector2 point = LcdMapping.LcdPointFromUv(new Vector2(0.5f, 0.5f), Vector2.zero, 4.46f, 1272f / 892f);
            Assert.AreEqual(0f, point.x, 0.0001f);
            Assert.AreEqual(0f, point.y, 0.0001f);
        }

        [Test]
        public void CornersMapToTheEdgesOfScreenBg()
        {
            float aspect = 1272f / 892f;
            Vector2 topRight = LcdMapping.LcdPointFromUv(Vector2.one, Vector2.zero, 4.46f, aspect);
            Assert.AreEqual(6.36f, topRight.x, 0.001f, "screen_bg is 12.72 wide");
            Assert.AreEqual(4.46f, topRight.y, 0.001f, "screen_bg is 8.92 tall");

            Vector2 bottomLeft = LcdMapping.LcdPointFromUv(Vector2.zero, Vector2.zero, 4.46f, aspect);
            Assert.AreEqual(-6.36f, bottomLeft.x, 0.001f);
            Assert.AreEqual(-4.46f, bottomLeft.y, 0.001f);
        }

        [Test]
        public void SceneCentreOffsetsThePoint()
        {
            Vector2 point = LcdMapping.LcdPointFromUv(new Vector2(0.5f, 0.5f), new Vector2(1000f, 0f), 4.46f, 1f);
            Assert.AreEqual(1000f, point.x, 0.0001f);
        }

        [Test]
        public void InsideRejectsUvOutsideTheFace()
        {
            Assert.IsTrue(LcdMapping.Inside(new Vector2(0f, 1f)));
            Assert.IsFalse(LcdMapping.Inside(new Vector2(1.01f, 0.5f)));
            Assert.IsFalse(LcdMapping.Inside(new Vector2(0.5f, -0.01f)));
        }
    }
}
