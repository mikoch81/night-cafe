using NightCafe.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace NightCafe.Tests
{
    /// <summary>
    /// The painted plank is one straight board from K1 to K5, while the lane's step path bends
    /// (it traces the RETRO rail). Cups drawn on the bent path floated above the board.
    /// </summary>
    public sealed class CupRailTests
    {
        const float Tolerance = 0.0001f;

        // LaneConfig defaults for the left-up lane: start, bend, end.
        static readonly Vector2 First = new(-5.84f, 2.14f);
        static readonly Vector2 Bend = new(-3.11f, 1.50f);
        static readonly Vector2 Last = new(-1.95f, 0.91f);

        [Test]
        public void RetroKeepsTheBentPath()
        {
            Assert.AreEqual(Bend, CupController.FootFor(Bend, First, Last, straightRail: false));
        }

        [Test]
        public void StraightRailDropsTheBendOntoTheBoard()
        {
            Vector2 foot = CupController.FootFor(Bend, First, Last, straightRail: true);
            float expectedY = Mathf.Lerp(First.y, Last.y, Mathf.InverseLerp(First.x, Last.x, Bend.x));
            Assert.AreEqual(Bend.x, foot.x, Tolerance, "the cup keeps its progress along the lane");
            Assert.AreEqual(expectedY, foot.y, Tolerance);
            Assert.Less(foot.y, Bend.y, "the bend bulges above the board; the foot comes down to it");
        }

        [Test]
        public void TheEndsAreOnTheBoardSoLaunchAndCatchDoNotMove()
        {
            Assert.AreEqual(First, CupController.FootFor(First, First, Last, straightRail: true));
            Assert.AreEqual(Last, CupController.FootFor(Last, First, Last, straightRail: true));
        }

        [Test]
        public void MirroredLaneWorksTheSameWay()
        {
            Vector2 first = new(-First.x, First.y), bend = new(-Bend.x, Bend.y), last = new(-Last.x, Last.y);
            Vector2 foot = CupController.FootFor(bend, first, last, straightRail: true);
            Vector2 mirror = CupController.FootFor(Bend, First, Last, straightRail: true);
            Assert.AreEqual(-mirror.x, foot.x, Tolerance);
            Assert.AreEqual(mirror.y, foot.y, Tolerance);
        }
    }
}
