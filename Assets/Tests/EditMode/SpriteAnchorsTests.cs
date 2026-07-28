using NightCafe.Config;
using NUnit.Framework;
using UnityEngine;

namespace NightCafe.Tests
{
    public sealed class SpriteAnchorsTests
    {
        [TestCase("barista_up")]
        [TestCase("barista_down")]
        [TestCase("barista_catch")]
        [TestCase("barista_miss")]
        public void AllBaristaPosesShareOnePivot(string pose)
        {
            Assert.IsTrue(SpriteAnchors.TryGet(pose, out Vector2 pivot));
            Assert.AreEqual(SpriteAnchors.Barista, pivot,
                "poses must share an anchor or the barista jumps sideways when the pose swaps");
        }

        [Test]
        public void BothCatFramesShareOnePivot()
        {
            Assert.IsTrue(SpriteAnchors.TryGet("cat_a", out Vector2 a));
            Assert.IsTrue(SpriteAnchors.TryGet("cat_b", out Vector2 b));
            Assert.AreEqual(a, b, "the mop swings between frames, the cat must not");
        }

        [Test]
        public void BaristaPivotSitsOnTheFeetLine()
        {
            // Content occupies rows 0..356 of a 400 px canvas, so the feet are 44 px
            // above the bottom edge: 44 / 400 = 0.11 in normalised space.
            Assert.AreEqual(44f / 400f, SpriteAnchors.Barista.y, 0.0001f);
        }

        [Test]
        public void CupPivotIsHorizontallyCentred()
        {
            Assert.IsTrue(SpriteAnchors.TryGet("cup", out Vector2 pivot));
            Assert.AreEqual(0.5f, pivot.x, 0.0001f, "cup content is centred in its canvas");
        }

        [Test]
        public void BrokenCupSharesTheCupHorizontalAnchor()
        {
            Assert.IsTrue(SpriteAnchors.TryGet("cup", out Vector2 cup));
            Assert.IsTrue(SpriteAnchors.TryGet("cup_broken", out Vector2 broken));
            Assert.AreEqual(cup.x, broken.x, 0.0001f, "shards must land where the cup was");
        }

        [Test]
        public void UnknownSpriteKeepsCentredPivot()
        {
            Assert.IsFalse(SpriteAnchors.TryGet("device_shell", out _));
            Assert.IsFalse(SpriteAnchors.TryGet("button_normal", out _));
        }

        [Test]
        public void EveryPivotIsInsideTheCanvas()
        {
            foreach (string name in new[]
                     {
                         "barista_up", "barista_down", "barista_catch", "barista_miss",
                         "cat_a", "cat_b", "cup", "cup_broken", "machine_head", "stain"
                     })
            {
                Assert.IsTrue(SpriteAnchors.TryGet(name, out Vector2 pivot));
                Assert.That(pivot.x, Is.InRange(0f, 1f), $"{name} pivot x");
                Assert.That(pivot.y, Is.InRange(0f, 1f), $"{name} pivot y");
            }
        }
    }
}
